#!/bin/env python
# -*- coding: utf-8 -*-
##
# utils.py: Common functions for use in IQ# Python tests.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

import importlib
import os
import qsharp
import sys

def set_environment_variables():
    '''
    Sets environment variables for test execution and restarts the IQ# kernel.
    Also changes the working directory to the qsharp/tests folder
    so that the `Operations.qs` file will be correctly imported/loaded when
    the `qsharp` module reloads.
    '''        
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    os.environ["AZURE_QUANTUM_ENV"] = "mock"
    os.environ["IQSHARP_AUTO_LOAD_PACKAGES"] = "$null"
    importlib.reload(qsharp)
    if "qsharp.chemistry" in sys.modules:
        importlib.reload(qsharp.chemistry)
