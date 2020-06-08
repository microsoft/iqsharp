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
    'execute',
    'status',
    'output',
    'jobs'
]

## FUNCTIONS ##

def connect(**params) -> Any:
    return qsharp.client._execute_magic(f"azure.connect", raise_on_stderr=False, **params)

def target(name : str = '', **params) -> Any:
    return qsharp.client._execute_magic(f"azure.target {name}", raise_on_stderr=False, **params)

def submit(op, **params) -> Any:
    return qsharp.client._execute_callable_magic("azure.submit", op, raise_on_stderr=False, **params)

def execute(op, **params) -> Any:
    return qsharp.client._execute_callable_magic("azure.execute", op, raise_on_stderr=False, **params)

def status(jobId : str = '', **params) -> Any:
    return qsharp.client._execute_magic(f"azure.status {jobId}", raise_on_stderr=False, **params)

def output(jobId : str = '', **params) -> Any:
    return qsharp.client._execute_magic(f"azure.output {jobId}", raise_on_stderr=False, **params)

def jobs(**params) -> Any:
    return qsharp.client._execute_magic(f"azure.jobs", raise_on_stderr=False, **params)
