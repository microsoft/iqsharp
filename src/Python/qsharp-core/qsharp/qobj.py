# -*- coding: utf-8 -*-
##
# qobj.py: Functions for converting to and from QuTiP representations of
#     quantum objects.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

from typing import Optional
import warnings
import numpy as np
import qutip as qt

def convert_diagnostic_to_qobj(data) -> Optional[qt.Qobj]:
    """
    Given data deserialized from JSON diagnostics emitted by a simulator,
    attempts to convert to a QuTiP quantum object, returning the converted
    object if possible and `None` otherwise.
    """

    # Try to convert data to a Qobj if possible.
    if "amplitudes" in data:
        # Got a state vector, so convert to a Qobj with type=ket.
        # The serialization for these state vectors is defined at:
        # https://github.com/microsoft/iqsharp/blob/1015192aedababc3fe6d64e6def5838ea5eaab2f/src/Jupyter/Visualization/StateDisplayEncoders.cs#L113

        # We start by importing SciPy (we know it's available since it's
        # a hard dependency of QuTiP, and QuTiP has been successfully imported
        # at this point).
        from scipy.sparse import csr_matrix
        flat = list(data['amplitudes'].items())
        csr_data = np.array([z['Real'] + 1j * z['Imaginary'] for idx, z in flat])
        row_idxs = np.array([int(idx) for idx, z in flat])
        col_idxs = np.zeros((len(csr_data),), dtype=int)
        arr = csr_matrix(
            (csr_data, (row_idxs, col_idxs))
        )
        return qt.Qobj(
            arr,
            dims=[[2] * data['n_qubits'], [1] * data['n_qubits']]
        )

    # The State UDT case for density operators as represented in C# looks like:
    # {n_qubits: ..., data: {Mixed: {...}}}
    # Thus, we look for something that has both n_qubits and data, then look
    # for what the only key in data is.
    elif 'n_qubits' in data and 'data' in data:
        n_qubits = data['n_qubits']
        kind = list(data['data'].keys())[0]

        # TODO: Support kinds other than Mixed.
        if kind != 'Mixed':
            return None

        state_data = data['data'][kind]

        # Got a density operator, so convert to a Qobj with type=oper.
        # The serialization for these density operators is defined at:
        # https://github.com/microsoft/qsharp-runtime/blob/1334dc8cefb447e65feca66c463bcd77421bd5a2/src/Simulation/Simulators/OpenSystemsSimulator/DataModel/State.cs#L99
        arr = np.array(state_data['data']).reshape((2 ** n_qubits, ) * 2 + (2, ))
        qobj = qt.Qobj(
            arr[..., 0] + 1j * arr[..., 1],
            dims=[[2] * n_qubits] * 2
        )
        if not qobj.isherm or abs(qobj.tr() - 1.0) >= 1e-8:
            warnings.warn("Expected a density operator, but failed hermicity and/or trace check.")
        return qobj

    elif 'Qubits' in data and 'Data' in data:
        # Got a unitary operator, so convert to a Qobj with type=oper.
        # TODO: Find a better way of identifying unitary operators.
        #       This is very much so a hack.
        # The serialization for these unitary operators is defined at:
        # https://github.com/microsoft/QuantumLibraries/blob/687692d75af05709f0f418f8f9715cdd67c9e572/Standard/src/Diagnostics/Emulation/DataStructures.cs#L19
        n_qubits = len(data['Qubits'])
        arr = np.array(data['Data']).reshape((2 ** n_qubits, ) * 2 + (2, ))
        return qt.Qobj(
            arr[..., 0] + 1j * arr[..., 1],
            dims=[[2] * n_qubits] * 2
        )

    print(f"Got unexpected diagnostic {data}.")

    return None
