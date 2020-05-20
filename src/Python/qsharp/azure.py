#!/bin/env python
# -*- coding: utf-8 -*-
##
# azure.py: enables using Q# quantum execution on Azure Quantum from Python.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

## IMPORTS ##

import qsharp
import json
import typing
from typing import List, Dict, Callable, Any

from qsharp.serialization import map_tuples
from typing import List, Tuple, Dict, Iterable
from enum import Enum

## LOGGING ##

import logging
logger = logging.getLogger(__name__)

## EXPORTS ##

__all__ = [
    'connect',
    'target',
    'submit',
    'status'
]

## FUNCTIONS ##

def connect(**params) -> Any:
    return qsharp.client._execute_magic(f"connect", raise_on_stderr=False, **params)

def target(name : str = '', **params) -> Any:
    return qsharp.client._execute_magic(f"target {name}", raise_on_stderr=False, **params)

def submit(op, **params) -> Any:
    return qsharp.client._execute_callable_magic("submit", op, raise_on_stderr=False, **params)

def status(jobId : str = '', **params) -> Any:
    return qsharp.client._execute_magic(f"status {jobId}", raise_on_stderr=False, **params)
