#!/bin/env python
# -*- coding: utf-8 -*-
##
# noise_model.py: Data model and interfaces for open systems simulation noise
#     models.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

from __future__ import annotations
from email import generator

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

from typing import Any, List, Optional, Tuple, Union, TYPE_CHECKING
import qsharp
import json
import dataclasses


## EXPORTS ##

__all__ = [
    "get_noise_model",
    "set_noise_model",
    "get_noise_model_by_name",
    "set_noise_model_by_name",

    # State data model
    "Stabilizer",

    # Process data model
    "SequenceProcess",
    "MixedPauliProcess",
    "ChpDecompositionProcess",
    "UnsupportedProcess",

    # Generator data model
    "ExplicitEigenvalueDecomposition",
    "UnsupportedGenerator",
    "GeneratorCoset",

    # CHP decomposition data model
    "Hadamard",
    "Cnot",
    "Phase",
    "AdjointPhase"
]

## TYPE DEFINITIONS ##

if TYPE_CHECKING:
    import qutip
    import numpy as np
    State = Union[qutip.Qobj, "Stabilizer"]
    Process = Union[qutip.Qobj, "ChpDecompositionProcess", "MixedPauliProcess", "SequenceProcess"]
    Instrument = Any # TODO
    Generator = Union["ExplicitEigenvalueDecomposition", "UnsupportedGenerator"]

## PUBLIC FUNCTIONS ##

def get_noise_model():
    """
    Returns the current noise model used in simulating Q# programs with the
    `.simulate_noise` method.
    """
    noise_model = convert_to_arrays(qsharp.client.get_noise_model())
    # Convert {"Mixed": ...} and so forth to qobj.
    return NoiseModel(**convert_to_qobjs(noise_model))

def get_noise_model_by_name(name: str):
    """
    Returns the built-in noise model with a given name.

    :param name: The name of the noise model to be returned (either `ideal`
        or `ideal_stabilizer`).
    """
    json_data = qsharp.client.get_noise_model_by_name(name)
    noise_model = convert_to_arrays(json_data)
    # Convert {"Mixed": ...} and so forth to qobj.
    return NoiseModel(**convert_to_qobjs(noise_model))

def set_noise_model(noise_model: NoiseModel):
    """
    Sets the current noise model used in simulating Q# programs with the
    `.simulate_noise` method.
    """
    json_data = dumps(convert_to_rust_style(noise_model._as_qobj()))
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
class NoiseModel:
    initial_state: State
    cnot: Process
    i: Process
    s: Process
    s_adj: Process
    t: Process
    t_adj: Process
    h: Process
    x: Process
    y: Process
    z: Process
    z_meas: Instrument
    rx: GeneratorCoset
    ry: GeneratorCoset
    rz: GeneratorCoset

    def _as_qobj(self):
        return {
            'initial_state': self.initial_state,
            'cnot': self.cnot,
            'i': self.i,
            's': self.s,
            's_adj': self.s_adj,
            't': self.t,
            't_adj': self.t_adj,
            'h': self.h,
            'x': self.x,
            'y': self.y,
            'z': self.z,
            'z_meas': self.z_meas,
            'rx': self.rx,
            'ry': self.ry,
            'rz': self.rz,
        }

@dataclasses.dataclass
class Stabilizer:
    n_qubits: int
    table: np.ndarray

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': {
                'Stabilizer': {
                    'n_qubits': self.n_qubits,
                    # We don't use arr_to_rust_style here, as that is designed
                    # for complex arrays. Rather, we depend on the default
                    # set in as_jobj, below.
                    'table': self.table
                }
            }
        }

@dataclasses.dataclass
class UnsupportedProcess():
    n_qubits: int

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': 'Unsupported'
        }

@dataclasses.dataclass
class ExplicitEigenvalueDecomposition():
    values: Any
    vectors: Any
    n_qubits: int

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': {
                'ExplicitEigenvalueDecomposition': {
                    'values': arr_to_rust_style(self.values),
                    'vectors': arr_to_rust_style(self.vectors)
                }
            }
        }

@dataclasses.dataclass
class UnsupportedGenerator():
    n_qubits: int

    def _as_jobj(self):
        return {
            'n_qubits': self.n_qubits,
            'data': 'Unsupported'
        }

@dataclasses.dataclass
class GeneratorCoset():
    generator: Generator
    pre: Optional[Any]
    post: Optional[Any]

    def _as_jobj(self):
        obj = {}
        if self.pre is not None:
            obj['pre'] = self.pre
        if self.post is not None:
            obj['post'] = self.post
        obj['generator'] = self.generator._as_jobj()
        return obj

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

    @classmethod
    def _from_jobj(cls, obj):
        return cls(n_qubits=obj['n_qubits'], operations=[
            Hadamard(step['Hadamard'])
            if 'Hadamard' in step else
            Phase(step['Phase'])
            if 'Phase' in step else
            AdjointPhase(step['AdjointPhase'])
            if 'AdjointPhase' in step else
            Cnot(*step['Cnot'])
            for step in obj['data']['ChpDecomposition']
        ])

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


def jobj_to_generator(json_obj) -> GeneratorCoset:
    generator = json_obj['generator']
    if 'data' in generator:
        if generator['data'] == "Unsupported":
            generator = UnsupportedGenerator(generator['n_qubits'])
        elif 'ExplicitEigenvalueDecomposition' in generator['data']:
            generator = ExplicitEigenvalueDecomposition(
                values=generator['data']['ExplicitEigenvalueDecomposition']['values'],
                vectors=generator['data']['ExplicitEigenvalueDecomposition']['vectors'],
                n_qubits=generator['n_qubits']
            )

    return GeneratorCoset(
        generator=generator,
        pre=convert_to_qobjs(json_obj['pre']) if 'pre' in json_obj else None,
        post=convert_to_qobjs(json_obj['post']) if 'post' in json_obj else None
    )


def convert_to_qobjs(json_obj):
    import qutip as qt
    return (
        arr_to_qobj(json_obj['data']['Mixed'])
        if 'data' in json_obj and 'Mixed' in json_obj['data'] else

        arr_to_qobj(json_obj['data']['Unitary'])
        if 'data' in json_obj and 'Unitary' in json_obj['data'] else

        jobj_to_generator(json_obj)
        if 'generator' in json_obj else

        Stabilizer(json_obj['n_qubits'], json_obj['data']['Stabilizer']['table'])
        if 'data' in json_obj and 'Stabilizer' in json_obj['data'] else

        ChpDecompositionProcess._from_jobj(json_obj)
        if 'data' in json_obj and 'ChpDecomposition' in json_obj['data'] else

        # Since we deserialized unsupported generators above, we know that
        # this unsupported must be a process.
        UnsupportedProcess(json_obj['n_qubits'])
        if 'data' in json_obj and json_obj['data'] == "Unsupported" else

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
