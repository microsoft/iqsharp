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
from typing import List, Dict, Callable, Any, Union
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
    'AzureTarget',
    'AzureJob',
    'AzureError'
]

## CLASSES ##

class AzureTarget(object):
    """
    Represents an instance of an Azure Quantum execution target for Q# job submission.
    """
    def __init__(self, data: Dict):
        self.__dict__ = data
        self.id = data["id"]
        self.current_availability = data["current_availability"]
        self.average_queue_time = data["average_queue_time"]

    def __repr__(self) -> str:
        return self.__dict__.__repr__()

    def __eq__(self, other) -> bool:
        if not isinstance(other, AzureTarget):
            # don't attempt to compare against unrelated types
            return NotImplemented
        return self.__dict__ == other.__dict__

class AzureJob(object):
    """
    Represents an instance of an Azure Quantum job.
    """
    def __init__(self, data: Dict):
        self.__dict__ = data
        self.id = data["id"]
        self.name = data["name"]
        self.status = data["status"]
        self.provider = data["provider"]
        self.target = data["target"]
        self.creation_time = data["creation_time"]
        self.begin_execution_time = data["begin_execution_time"]
        self.end_execution_time = data["end_execution_time"]

    def __repr__(self) -> str:
        return self.__dict__.__repr__()

    def __eq__(self, other) -> bool:
        if not isinstance(other, AzureJob):
            # don't attempt to compare against unrelated types
            return NotImplemented
        return self.__dict__ == other.__dict__

class AzureError(Exception):
    """
    Contains error information resulting from an attempt to interact with Azure.
    """
    def __init__(self, data: Dict):
        self.__dict__ = data
        self.error_code = data["error_code"]
        self.error_name = data["error_name"]
        self.error_description = data["error_description"]

    def __repr__(self) -> str:
        return self.__dict__.__repr__()

    def __eq__(self, other) -> bool:
        if not isinstance(other, AzureError):
            # don't attempt to compare against unrelated types
            return NotImplemented
        return self.__dict__ == other.__dict__

## FUNCTIONS ##

def connect(**params) -> List[AzureTarget]:
    """
    Connects to an Azure Quantum workspace or displays current connection status.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.connect for more details.
    """
    result = qsharp.client._execute_magic(f"azure.connect", raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return [AzureTarget(target) for target in result]

def target(name : str = '', **params) -> AzureTarget:
    """
    Sets or displays the active execution target for Q# job submission in an Azure Quantum workspace.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.target for more details.
    """
    result = qsharp.client._execute_magic(f"azure.target {name}", raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return AzureTarget(result)

def submit(op : qsharp.QSharpCallable, **params) -> AzureJob:
    """
    Submits a job to an Azure Quantum workspace.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.submit for more details.
    """
    result = qsharp.client._execute_callable_magic("azure.submit", op, raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return AzureJob(result)

def execute(op : qsharp.QSharpCallable, **params) -> Dict:
    """
    Executes a job in an Azure Quantum workspace.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.execute for more details.
    """
    result = qsharp.client._execute_callable_magic("azure.execute", op, raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return result

def status(jobId : str = '', **params) -> AzureJob:
    """
    Displays status for a job in the current Azure Quantum workspace.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.status for more details.
    """
    result = qsharp.client._execute_magic(f"azure.status {jobId}", raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return AzureJob(result)

def output(jobId : str = '', **params) -> Dict:
    """
    Displays results for a job in the current Azure Quantum workspace.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.output for more details.
    """
    result = qsharp.client._execute_magic(f"azure.output {jobId}", raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return result

def jobs(filter : str = '', **params) -> List[AzureJob]:
    """
    Displays a list of jobs in the current Azure Quantum workspace.
    See https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.jobs for more details.
    """
    result = qsharp.client._execute_magic(f"azure.jobs \"{filter}\"", raise_on_stderr=False, **params)
    if "error_code" in result: raise AzureError(result)
    return [AzureJob(job) for job in result]
