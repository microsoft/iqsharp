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

    def __eq__(self, other):
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

    def __eq__(self, other):
        if not isinstance(other, AzureJob):
            # don't attempt to compare against unrelated types
            return NotImplemented
        return self.__dict__ == other.__dict__

class AzureError(object):
    """
    Contains error information resulting from an attempt to interact with Azure.
    """
    def __init__(self, data: Dict):
        self.__dict__ = data
        self.error_code = data["error_code"]
        self.error_name = data["error_name"]
        self.error_description = data["error_description"]

    def __eq__(self, other):
        if not isinstance(other, AzureError):
            # don't attempt to compare against unrelated types
            return NotImplemented
        return self.__dict__ == other.__dict__

## FUNCTIONS ##

def connect(**params) -> Union[List[AzureTarget], AzureError]:
    result = qsharp.client._execute_magic(f"azure.connect", raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else [AzureTarget(target) for target in result]

def target(name : str = '', **params) -> Union[AzureTarget, AzureError]:
    result = qsharp.client._execute_magic(f"azure.target {name}", raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else AzureTarget(result)

def submit(op, **params) -> Union[AzureJob, AzureError]:
    result = qsharp.client._execute_callable_magic("azure.submit", op, raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else AzureJob(result)

def execute(op, **params) -> Union[Dict, AzureError]:
    result = qsharp.client._execute_callable_magic("azure.execute", op, raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else result

def status(jobId : str = '', **params) -> Union[AzureJob, AzureError]:
    result = qsharp.client._execute_magic(f"azure.status {jobId}", raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else AzureJob(result)

def output(jobId : str = '', **params) -> Union[Dict, AzureError]:
    result = qsharp.client._execute_magic(f"azure.output {jobId}", raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else result

def jobs(**params) -> Union[List[AzureJob], AzureError]:
    result = qsharp.client._execute_magic(f"azure.jobs", raise_on_stderr=False, **params)
    return AzureError(result) if "error_code" in result else [AzureJob(job) for job in result]
