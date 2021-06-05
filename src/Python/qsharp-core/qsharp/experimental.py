#!/bin/env python
# -*- coding: utf-8 -*-
##
# iqsharp.py: Client for the IQ# Jupyter kernel.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

"""
This module allow for using experimental features of the Quantum Development Kit,
including noisy simulators for Q# programs.
"""

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

import qsharp
from qsharp.loader import QSharpCallable
import json

## EXPORTS ##

__all__ = [
    "enable_noisy_simulation",
    "get_noise_model",
    "set_noise_model",
    "get_noise_model_by_name",
    "set_noise_model_by_name"
]

## PUBLIC FUNCTIONS ##

def enable_noisy_simulation():
    """
    Enables the `.simulate_noise` method to be used on Python objects
    representing Q# operations, allowing for Q# programs to be simulated using
    experimental simulators.

    Noisy simulation is controlled by the :func:`~qsharp.experimental.get_noise_model`,
    :func:`~qsharp.experimental.set_noise_model` and :func:`~qsharp.experimental.set_noise_model_by_name`
    functions, and by the `opensim.nQubits` and `opensim.representation` keys
    of the :any:`qsharp.config` object.
    """

    # Try to import optional packages used by noise modeling.
    optional_dependencies = []
    try:
        import numpy as np
        optional_dependencies.append(f"numpy:{np.__version__}")
    except:
        np = None

    try:
        import qutip as qt
        optional_dependencies.append(f"qutip:{qt.__version__}")
    except:
        qt = None

    # Tell the kernel to turn on experimental features.
    try:
        content = {
            "feature_name": "noisy_simulation",
            "optional_dependencies": optional_dependencies
        }
        msg = qsharp.client.kernel_client.session.msg('iqsharp_python_enable_experimental', content)
        qsharp.client.kernel_client.shell_channel.send(msg)
    except:
        pass

    # Actually attach the new method to the type used for exposing Q# callables
    # to Python.
    def simulate_noise(self, **kwargs):
        return qsharp.client._simulate_noise(self, **kwargs)

    QSharpCallable.simulate_noise = simulate_noise


def get_noise_model():
    """
    Returns the current noise model used in simulating Q# programs with the
    `.simulate_noise` method.
    """
    noise_model = convert_to_arrays(qsharp.client._get_noise_model())
    # Convert {"Mixed": ...} and so forth to qobj.
    return convert_to_qobjs(noise_model)

def get_noise_model_by_name(name: str):
    """
    Returns the built-in noise model with a given name.

    :param name: The name of the noise model to be returned (either `ideal`
        or `ideal_stabilizer`).
    """
    noise_model = convert_to_arrays(qsharp.client._get_noise_model_by_name(name))
    # Convert {"Mixed": ...} and so forth to qobj.
    return convert_to_qobjs(noise_model)

def set_noise_model(noise_model):
    """
    Sets the current noise model used in simulating Q# programs with the
    `.simulate_noise` method.
    """
    qsharp.client._set_noise_model(json.dumps(convert_to_rust_style(noise_model)))

def set_noise_model_by_name(name):
    """
    Sets the current noise model used in simulating Q# programs with the
    `.simulate_noise` method to a built-in noise model, given by name.

    :param name: The name of the noise model to be returned (either `ideal`
        or `ideal_stabilizer`).
    """
    qsharp.client._set_noise_model_by_name(name)

## PRIVATE FUNCTIONS ##

def is_rust_style_array(json_obj):
    return (
        isinstance(json_obj, dict) and
        'v' in json_obj and
        'data' in json_obj and
        'dim' in json_obj and
        json_obj['v'] == 1
    )

def rust_style_array_as_array(json_obj):
    import numpy as np
    arr = np.array(json_obj['data']).reshape(json_obj['dim'] + [2]).astype(complex)
    return arr[..., 0] + 1j * arr[..., 1]

def convert_to_arrays(json_obj):
    return (
        # Add on a trailing index of length 2.
        rust_style_array_as_array(json_obj)
        if is_rust_style_array(json_obj) else
        {
            key:
                convert_to_arrays(value)
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
