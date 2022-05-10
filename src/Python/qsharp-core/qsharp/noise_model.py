#!/bin/env python
# -*- coding: utf-8 -*-
##
# noise_model.py: Data model and interfaces for open systems simulation noise
#     models.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##


## DESIGN NOTES ##

# The functions in this module may take dependencies on QuTiP and NumPy,
# while neither of those are dependencies of the qsharp package on the whole.
# To avoid those becoming hard dependencies, we do not import either package
# at the top of this module, but do so inside each function that uses those
# dependencies.
#
# This has some performance and code maintenance implications, but allows for
# being permissive with respect to dependencies.

## IMPORTS ##

from typing import Any, List, Tuple
import qsharp
import json
import dataclasses

from typing import Any, Union

## EXPORTS ##

__all__ = [
    "get_noise_model",
    "set_noise_model",
    "get_noise_model_by_name",
    "set_noise_model_by_name",

    # SequenceProcess process data model
    "SequenceProcess",

    # Mixed pauli data model
    "MixedPauliProcess",

    # CHP decomposition data model
    "ChpDecompositionProcess",
    "Hadamard",
    "Cnot",
    "Phase",
    "AdjointPhase"
]

## PUBLIC FUNCTIONS ##

def get_noise_model():
    """
    Returns the current noise model used in simulating Q# programs with the
    `.simulate_noise` method.
    """
    noise_model = convert_to_arrays(qsharp.client.get_noise_model())
    # Convert {"Mixed": ...} and so forth to qobj.
    return convert_to_qobjs(noise_model)

def get_noise_model_by_name(name: str):
    """
    Returns the built-in noise model with a given name.

    :param name: The name of the noise model to be returned (either `ideal`
        or `ideal_stabilizer`).
    """
    noise_model = convert_to_arrays(qsharp.client.get_noise_model_by_name(name))
    # Convert {"Mixed": ...} and so forth to qobj.
    return convert_to_qobjs(noise_model)

def set_noise_model(noise_model):
    """
    Sets the current noise model used in simulating Q# programs with the
    `.simulate_noise` method.
    """
    json_data = dumps(convert_to_rust_style(noise_model))
    qsharp.client.set_noise_model(json_data)

def set_noise_model_by_name(name):
    """
    Sets the current noise model used in simulating Q# programs with the
    `.simulate_noise` method to a built-in noise model, given by name.

    :param name: The name of the noise model to be returned (either `ideal`
        or `ideal_stabilizer`).
    """
    qsharp.client.set_noise_model_by_name(name)



## PUBLIC DATA MODEL ##

@dataclasses.dataclass
class SequenceProcess():
    n_qubits: int
    processes: List[Any]

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': {
                "Sequence": list(map(_as_jobj, self.processes))
            }
        }

@dataclasses.dataclass
class Hadamard():
    idx_target: int

    def _as_jobj(self):
        return {
            'Hadamard': self.idx_target
        }

@dataclasses.dataclass
class Phase():
    idx_target: int

    def _as_jobj(self):
        return {
            'Phase': self.idx_target
        }

@dataclasses.dataclass
class AdjointPhase():
    idx_target: int

    def _as_jobj(self):
        return {
            'AdjointPhase': self.idx_target
        }

@dataclasses.dataclass
class Cnot():
    idx_control: int
    idx_target: int

    def _as_jobj(self):
        return {
            'Cnot': [self.idx_control, self.idx_target]
        }

@dataclasses.dataclass
class ChpDecompositionProcess():
    n_qubits: int
    operations: List[Union[Hadamard, Cnot, Phase, AdjointPhase]]

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': {
                'ChpDecomposition': list(map(_as_jobj, self.operations))
            }
        }

@dataclasses.dataclass
class MixedPauliProcess():
    n_qubits: int
    operators: List[Tuple[float, Union[List[qsharp.Pauli], str]]]

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': {
                'MixedPauli': [
                    [
                        pr,
                        [
                            (qsharp.Pauli[p] if isinstance(p, str) else qsharp.Pauli(p)).name
                            for p in ops
                        ]
                    ]
                    for (pr, ops) in self.operators
                ]
            }
        }

## PRIVATE FUNCTIONS ##

literal_keys = frozenset(['ChpDecomposition'])

def _as_jobj(o, default=lambda x: x):
    import numpy as np
    if isinstance(o, np.ndarray):
        # Use Rust-style arrays.
        return {
            'v': 1,
            'dim': list(o.shape),
            'data': o.reshape((-1, )).tolist()
        }
    elif hasattr(o, '_as_jobj'):
        return o._as_jobj()

    return default(o)

class NoiseModelEncoder(json.JSONEncoder):
    def default(self, o: Any) -> Any:
        return _as_jobj(o, super().default)

def dumps(obj: Any) -> str:
    """
    Wraps json.dumps with a custom JSONEncoder class to cover types used in
    noise model serialization.
    """
    return json.dumps(
        obj=obj,
        cls=NoiseModelEncoder
    )

def is_rust_style_array(json_obj):
    return (
        isinstance(json_obj, dict) and
        'v' in json_obj and
        'data' in json_obj and
        'dim' in json_obj and
        json_obj['v'] == 1
    )

def rust_style_array_as_array(json_obj, as_complex : bool = True):
    import numpy as np
    dims = json_obj['dim'] + [2] if as_complex else json_obj['dim']
    arr = np.array(json_obj['data']).reshape(dims)
    if as_complex:
        arr = arr.astype(complex)
        return arr[..., 0] + 1j * arr[..., 1]
    else:
        return arr

def convert_to_arrays(json_obj, as_complex : bool = True):
    return (
        # Add on a trailing index of length 2.
        rust_style_array_as_array(json_obj, as_complex=as_complex)
        if is_rust_style_array(json_obj) else
        {
            key:
                value
                if key in literal_keys else

                convert_to_arrays(value, as_complex=key != 'table')
                if isinstance(value, dict) else

                [convert_to_arrays(element) for element in value]
                if isinstance(value, list) else

                value

            for key, value in json_obj.items()
        }
    )

def arr_to_qobj(arr):
    import qutip as qt
    import numpy as np
    return qt.Qobj(arr, dims=[[2] * int(np.log2(arr.shape[1]))] * 2)

def convert_to_qobjs(json_obj):
    import qutip as qt
    return (
        arr_to_qobj(json_obj['data']['Mixed'])
        if 'data' in json_obj and 'Mixed' in json_obj['data'] else

        arr_to_qobj(json_obj['data']['Unitary'])
        if 'data' in json_obj and 'Unitary' in json_obj['data'] else

        qt.kraus_to_super([arr_to_qobj(op) for op in json_obj['data']['KrausDecomposition']])
        if 'data' in json_obj and 'KrausDecomposition' in json_obj['data'] else

        {
            key:
                value
                if key in literal_keys else

                # Recurse if needed...
                convert_to_qobjs(value)
                if isinstance(value, dict) else

                [convert_to_qobjs(element) for element in value]
                if isinstance(value, list) else

                # Just return value if there's nothing else to do.
                value
            for key, value in json_obj.items()
        }
    )

def arr_to_rust_style(arr):
    import numpy as np
    return {
        'v': 1,
        'dim': list(arr.shape),
        'data': np.moveaxis(np.array([arr.real, arr.imag]), 0, -1).reshape((-1, 2)).tolist()
    }

def qobj_to_rust_style(qobj, expect_state=False):
    import qutip as qt
    import numpy as np
    data = None
    n_qubits = 1
    # Figure out what kind of qobj we have and convert accordingly.
    if qobj.type == 'oper':
        n_qubits = len(qobj.dims[0])
        data = {
            'Mixed' if expect_state else 'Unitary':
            arr_to_rust_style(qobj.data.todense())
        }
    elif qobj.type == 'super':
        n_qubits = len(qobj.dims[0][0])
        data = {
            'KrausDecomposition': arr_to_rust_style(
                np.array([
                    op.data.todense()
                    for op in qt.to_kraus(qobj)
                ])
            )
        }
    return {
        "n_qubits": n_qubits,
        "data": data
    }

def convert_to_rust_style(json_obj, expect_state=False):
    import qutip as qt
    return (
        qobj_to_rust_style(json_obj, expect_state=expect_state)
        if isinstance(json_obj, qt.Qobj) else

        list(map(convert_to_rust_style, json_obj))
        if isinstance(json_obj, list) else

        {
            key: convert_to_rust_style(value, expect_state=key == 'initial_state')
            for key, value in json_obj.items()
        }
        if isinstance(json_obj, dict) else

        json_obj
    )
