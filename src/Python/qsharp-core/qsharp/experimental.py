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

import warnings

# Re-export noise model APIs from their new locations.
from qsharp.noise_model import *

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
    warnings.warn(
        "The open systems simulation feature is no longer experimental, such that enable_noisy_simulation "
        "no longer needs to be called before using simulate_noise. This function will be removed in "
        "a future version of the qsharp package.",
        DeprecationWarning
    )
