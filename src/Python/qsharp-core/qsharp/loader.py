#!/bin/env python
# -*- coding: utf-8 -*-
##
# loader.py: Support for exposing Q# namespaces as Python modules.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

import sys
import logging
from types import ModuleType, new_class
from importlib.abc import MetaPathFinder, Loader
from typing import Iterable, Optional, Any, Dict, Tuple

import qsharp
from qsharp.clients.iqsharp import IQSharpError

logger = logging.getLogger(__name__)


class QSharpModuleFinder(MetaPathFinder):
    def find_module(self, full_name : str, path : Optional[str] = None) -> Loader:
        # We expose Q# namespaces as their own root-level packages.
        # E.g.:
        #     >>> import Microsoft.Quantum.Intrinsic as mqi
        # Thus, we need to check if the full name is one that that we can
        # sensibly load before we proceed.

        # To check the full name, we ask the client rather than going through
        # the public API for the qsharp package, so that we can check if the
        # client is currently busy. This can happen if anything below us in
        # meta_path needs to handle an import during an execute; this is the
        # case when ZeroMQ needs to import additional functionality from a
        # Cython module to handle a message.
        # See https://github.com/Microsoft/QuantumLibraries/issues/69 for an
        # example of this failure modality.

        # If the client is busy, we'll want to forego this request to find a
        # module and return None early.
        if qsharp.client.busy:
            return None

        # At this point, we should be safe to rely on the public API again.
        ops = qsharp.get_available_operations_by_namespace()

        if full_name not in ops:
            # We may have been given part of the qualified name of a namespace.
            # E.g., if we try to import Microsoft.Quantum.Intrinsic, we'll
            # see calls with "Microsoft" and "Microsoft.Quantum" first.
            if not any(
                ns_name.startswith(full_name + ".")
                for ns_name in ops
            ):
                return None

        return QSharpModuleLoader()

class QSharpModuleLoader(Loader):
    def load_module(self, full_name : str):
        logger.debug(f"Trying to load {full_name} as a Q# namespace.")
        if full_name in sys.modules:
            return sys.modules[full_name]

        module = QSharpModule(full_name, full_name, self)

        # Register the new module.
        sys.modules.setdefault(full_name, module)
        return module

class QSharpCallable(object):
    _name : str
    def __init__(self, callable_name : str, source : str):
        self._name = callable_name
        self.source = source

    def __repr__(self) -> str:
        return f"<Q# callable {self._name}>"

    def __call__(self, **kwargs) -> Any:
        """
        Executes this function or operation on the QuantumSimulator target
        machine, returning its output as a Python object.
        """
        return self.simulate(**kwargs)

    def simulate(self, **kwargs) -> Any:
        """
        Executes this function or operation on the QuantumSimulator target
        machine, returning its output as a Python object.
        """
        return qsharp.client.simulate(self, **kwargs)

    def simulate_sparse(self, **kwargs) -> Any:
        """
        Executes this function or operation on the sparse simulator, returning
        its output as a Python object.
        """
        return qsharp.client.simulate_sparse(self, **kwargs)

    def toffoli_simulate(self, **kwargs) -> Any:
        """
        Executes this function or operation on the ToffoliSimulator target
        machine, returning its output as a Python object.
        """
        return qsharp.client.toffoli_simulate(self, **kwargs)

    def trace(self, **kwargs) -> Any:
        """
        Returns a structure representing the set of gates and qubits
        used to execute this operation.
        """
        return qsharp.client.trace(self, **kwargs)
    
    def simulate_noise(self, **kwargs) -> Any:
        """
        Executes this function or operation on the open systems simulator,
        using the currently set noise model and returning its output as a
        Python object.
        """
        return qsharp.client.simulate_noise(self, **kwargs)

    def as_qir(self, **kwargs) -> str:
        """
        Returns the QIR representation of the callable,
        assuming the callable is an entry point.

        This method uses `%qir` magic command, which accepts the
        following (optional) kwargs parameters:
        - `target`: The intended execution target for the compiled entrypoint.
                    Defaults to the active Azure Quantum target (which can be set with `%azure.target`).
                    Otherwise, defaults to a generic target, which may not work when running on a specific target.
        - `target_capability`: The capability of the intended execution target.
                               If `target` is specified or there is an active Azure Quantum target, defaults to the target's maximum capability.
                               Otherwise, defaults to `FullComputation`, which may not be supported when running on a specific target.
        - `output_format`: the QIR output format. Defaults to `IR`.
          Possible options are:
            * `IR`: Human-readable Intermediate Representation in plain-text
            * `Bitcode`: LLVM bitcode (only when writing to a output file)
            * `BitcodeBase64`: LLVM bitcode encoded as Base64
        - `output_file`: The file path for where to save the output QIR.

        :rtype: bytes
        """
        data = qsharp.client.compile_to_qir(self,
                                            **kwargs)
        if data and ("Text" in data):
            return data['Text']
        raise IQSharpError([f'Error in generating QIR. {data}'])

    def _repr_qir_(self, **kwargs: Any) -> bytes:
        """
        Returns the QIR representation of the callable,
        assuming the callable is an entry point.

        This method uses `%qir` magic command, which accepts the
        following (optional) kwargs parameters:
        - `target`: The intended execution target for the compiled entrypoint.
                    Defaults to the active Azure Quantum target (which can be set with `%azure.target`).
                    Otherwise, defaults to a generic target, which may not work when running on a specific target.
        - `target_capability`: The capability of the intended execution target.
                               If `target` is specified or there is an active Azure Quantum target, defaults to the target's maximum capability.
                               Otherwise, defaults to `FullComputation`, which may not be supported when running on a specific target.

        :rtype: bytes
        """
        # We need to use the Base64 encoding to be able to transfer
        # the bitcode via the Jupyter protocol
        qir_bitcodeBase64 = self.as_qir(output_format="BitcodeBase64",
                                        **kwargs)
        import base64
        qir_bitcode = base64.b64decode(qir_bitcodeBase64)
        return qir_bitcode


class QSharpModule(ModuleType):
    _qs_name : str

    def __init__(self, full_name : str, qs_name : str, loader : QSharpModuleLoader):
        super().__init__(full_name)
        self._qs_name = qs_name
        self.__file__ = f"qsharp:{qs_name}"
        self.__path__ = []
        self.__loader__ = loader

    def _all_sub_namespaces_as_parts(self) -> Iterable[Tuple[str]]:
        qs_namespaces = qsharp.get_available_operations_by_namespace().keys()
        all_namespaces = set()
        for ns in qs_namespaces:
            parts = tuple(ns.split("."))
            all_namespaces.add(parts)
            for idx_part in range(1, len(parts)):
                all_namespaces.add(tuple(parts[:-idx_part]))

        return all_namespaces

    def _immediate_sub_namespaces(self) -> Iterable[str]:
        parts = tuple(self._qs_name.split("."))
        for ns in self._all_sub_namespaces_as_parts():
            if ns[:len(parts)] == parts and len(ns) > len(parts):
                yield ns[len(parts)]

    def _all_sub_namespaces(self) -> Iterable[str]:
        return [
            ".".join(ns)
            for ns in self._all_sub_namespaces_as_parts()
            if ".".join(ns).startswith(self._qs_name + ".")
        ]

    def __dir__(self) -> Iterable[str]:
        ops = qsharp.get_available_operations_by_namespace()
        return list(sorted(
            list(self._immediate_sub_namespaces()) + 
            ops.get(self._qs_name, [])
        ))

    def __getattr__(self, name):
        ops = qsharp.get_available_operations_by_namespace()
        # NB: Our Q# namespace name may not exist as a key, as the namespace
        #     name may be a prefix (e.g.: `Microsoft` and `Microsoft.Quantum.`
        #     may be empty, even though `Microsoft.Quantum.Intrinsic` is not).
        #
        #     Thus, we need to look for sub-namespaces as well as callables.
        #     While subnamespaces aren't a concept in Q#, they are in Python,
        #     and are needed to make tab completion on imports work correctly.
        #
        #     Start by looking for sub-namespaces.
        sub_namespaces = list(self._all_sub_namespaces())
        qualified_name = f"{self._qs_name}.{name}"
        if qualified_name in sub_namespaces:
            return self.__loader__.load_module(qualified_name)

        if self._qs_name in ops and name in ops[self._qs_name]:
            op_cls = new_class(name, (QSharpCallable, ))

            # Copy over metadata from the operation's header.
            metadata = qsharp.client.get_operation_metadata(f"{self._qs_name}.{name}")
            op_cls.__doc__ = metadata.get('documentation', '')
            op_cls.__file__ = metadata.get('source', None)
            return op_cls(f"{self._qs_name}.{name}", "workspace")
        raise AttributeError(f"Q# namespace {self._qs_name} does not contain a callable {name}.")

    def __repr__(self) -> str:
        return f"<module '{self._qs_name}' (Q# namespace)>"
