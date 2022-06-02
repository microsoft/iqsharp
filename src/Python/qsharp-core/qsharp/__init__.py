#!/bin/env python
# -*- coding: utf-8 -*-
##
# __init__.py: Root module for the qsharp package.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

"""

"""

## IMPORTS ##

import sys
from contextlib import contextmanager
from typing import Any, List, Dict, Union
from collections import defaultdict
from distutils.version import LooseVersion

from qsharp.clients import _start_client
from qsharp.clients.iqsharp import IQSharpError
from qsharp.loader import QSharpCallable, QSharpModuleFinder
from qsharp.config import Config
from qsharp.packages import Packages
from qsharp.projects import Projects
from qsharp.types import Result, Pauli
from qsharp.utils import ImportFailure, try_import_qutip
try:
    from qsharp.version import __version__
except:
    __version__ = "<unknown>"

## EXPORTS ##

__all__ = [
    'compile', 'reload',
    'get_available_operations', 'get_available_operations_by_namespace',
    'get_workspace_operations',
    'config',
    'packages',
    'projects',
    'IQSharpError',
    'Result', 'Pauli'
]

## FUNCTIONS ##

def compile(code : str) -> Union[None, QSharpCallable, List[QSharpCallable]]:
    """
    Given a string containing Q# source code, compiles it into the current
    workspace and returns one or more Q# callable objects that can be used to
    invoke the new code.

    :param code: A string containing Q# source code to be compiled.
    :returns: A list of callables compiled from `code`, or a callable if exactly
        one callable is found.
    """
    compiled = client.compile(code)
    if compiled is None:
        return None

    ops = [
        QSharpCallable(op, "snippets")
        for op in compiled
    ]
    if len(ops) == 1:
        return ops[0]
    else:
        return ops


def reload() -> None:
    """
    Reloads the current IQ# workspace, recompiling source files in the
    workspace.

    If the workspace fails to compile (e.g., because of a missing package),
    Q# compilation errors are raised as an exception.
    """
    client.reload()

def get_available_operations() -> List[str]:
    """
    Returns a list containing the names of all operations and functions defined
    in the current workspace, and that have been compiled dynamically from
    snippets.
    """
    return client.get_available_operations()

def get_workspace_operations() -> List[str]:
    """
    Returns a list containing the names of all operations and functions defined
    in the current workspace, excluding dynamically compiled snippets.
    """
    return client.get_workspace_operations()

def get_available_operations_by_namespace() -> Dict[str, List[str]]:
    """
    Returns a dictionary from namespaces to lists of operations and functions
    defined in the current workspace that are members of each namespace.
    """
    ops = get_available_operations()
    by_ns = defaultdict(list)

    for qualified_name in ops:
        idx_last_dot = qualified_name.rfind(".")
        ns_name = qualified_name[:idx_last_dot]
        op_name = qualified_name[idx_last_dot + 1:]

        by_ns[ns_name].append(op_name)

    return dict(by_ns.items())

def component_versions() -> Dict[str, LooseVersion]:
    """
    Returns a dictionary from components of the IQ# kernel to their
    versions.
    """
    versions = client.component_versions()
    # Add in the qsharp Python package itself.
    versions["qsharp"] = LooseVersion(__version__)
    # If any experimental features are enabled, report them here.
    if _experimental_versions is not None:
        versions['experimental'] = _experimental_versions
    return versions

@contextmanager
def capture_diagnostics(passthrough: bool = False, as_qobj: bool = False) -> List[Any]:
    """
    Returns a context manager that captures diagnostics output from running Q#
    programs into a list.

    For example, to capture `DumpMachine` calls from a Q# operation run on the
    full-state simulator:

    .. code-block:: qsharp

        namespace Sample {
            open Microsoft.Quantum.Intrinsic;
            open Microsoft.Quantum.Diagnostics;

            operation RunMain() : Unit {
                use q = Qubit();
                within {
                    H(q);
                } apply {
                    DumpMachine();
                }
            }
        }

    .. code-block:: python

        import qsharp
        from Sample import RunMain

        with qsharp.capture_diagnostics() as diagnostics:
            RunMain.simulate()

        print(len(diagnostics)) # will print 1

    :param passthrough: If `True`, captured diagnostics will also be displayed
        as normal. By default, diagnostic outputs captured by this context
        manager will not be displayed.
    :param as_qobj: If `True`, this context manager will attempt to convert
        captured diagnostics representing quantum states and operations into
        QuTiP objects. This option requires that QuTiP is installed and
        can be imported.
    """
    # Before proceeding, check that if we were asked to convert to qobj data
    # that we can actually import qutip.
    if as_qobj:
        # We don't actually need QuTiP here, but are only importing to capture
        # exceptions as early as possible so as to provide actionable error
        # messages to the user.
        qt = try_import_qutip()
        if isinstance(qt, ImportFailure):
            raise ImportError("as_qobj was set to `True`, but cannot convert captured diagnostics to QObj since QuTiP failed to import.") from qt.cause

        from qsharp.qobj import convert_diagnostic_to_qobj

    processed_data = []
    with client.capture_diagnostics(passthrough=passthrough) as data:
        yield processed_data

        # Apply any postprocessing needed here and append to processed_data.
        for diagnostic in data:
            if as_qobj:
                converted = convert_diagnostic_to_qobj(diagnostic)
                if converted is not None:
                    diagnostic = converted
            processed_data.append(diagnostic)

## STARTUP ##

client = _start_client()
config = Config(client)
packages = Packages(client)
projects = Projects(client)
_experimental_versions = None

# Make sure that we're last on the meta_path so that actual modules are loaded
# first.
sys.meta_path.append(QSharpModuleFinder())

# If using IPython, forward some useful IQ# magic commands as IPython magic
# commands and define a couple new magic commands for IPython.
try:
    if __IPYTHON__:
        import qsharp.ipython_magic
        qsharp.ipython_magic.register_magics()
except NameError:
    pass

# Needed to recognize PEP 420 packages as subpackages.
import pkg_resources
pkg_resources.declare_namespace(__name__)