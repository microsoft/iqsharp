#!/bin/env python
# -*- coding: utf-8 -*-
##
# types.py: Provides types for interoperability with Q#.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

from enum import IntEnum
import random

from qsharp.utils import try_import_qutip
qt = try_import_qutip(optional=True)

## ENUMS ######################################################################

class Result(IntEnum):
    Zero = 0
    One = 1

    @classmethod
    def sample(cls):
        return random.choice(list(cls))

class Pauli(IntEnum):
    I = 0b00
    X = 0b01
    Y = 0b11
    Z = 0b10

    @classmethod
    def sample(cls):
        return random.choice(list(cls))

    if qt is not None:
        def as_qobj(self):
            if self == Pauli.I:
                return qt.qeye(2)
            elif self == Pauli.X:
                return qt.sigmax()
            elif self == Pauli.Y:
                return qt.sigmay()
            elif self == Pauli.Z:
                return qt.sigmaz()
            else:
                raise ValueError(f"Unrecognized Pauli value {self}.")
