#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_azure.py: Tests Azure Quantum functionality against a mock workspace.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

import importlib
import os
import pytest
import qsharp
from qsharp.azure import AzureError, AzureJob, AzureTarget
import sys

## SETUP ##

@pytest.fixture(scope="session", autouse=True)
def set_environment_variables():
    # Need to restart the IQ# kernel after setting the environment variable
    os.environ["AZURE_QUANTUM_ENV"] = "mock"
    importlib.reload(qsharp)
    if "qsharp.chemistry" in sys.modules:
        importlib.reload(qsharp.chemistry)

## TESTS ##

def test_empty_workspace():
    """
    Tests behavior of a mock workspace with no providers.
    """
    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.target()
    assert exception_info.value.error_name == "NotConnected"

    targets = qsharp.azure.connect(
        storage="test",
        subscription="test",
        resourceGroup="test",
        workspace="test"
    )
    assert targets == []

    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.target("invalid.target")
    assert exception_info.value.error_name == "InvalidTarget"

    jobs = qsharp.azure.jobs()
    assert jobs == []

def test_create_workspace_with_parameters():
    """
    Tests creation of a mock workspace with mock providers using parameters.
    """
    targets = qsharp.azure.connect(
        storage="test",
        subscription="test",
        resourceGroup="test",
        workspace="WorkspaceNameWithMockProviders"
    )
    assert isinstance(targets, list)
    assert len(targets) > 0

def test_workspace_with_providers():
    """
    Tests behavior of a mock workspace created via resource ID with mock providers.
    """
    subscriptionId = "test"
    resourceGroupName = "test"
    workspaceName = "WorkspaceNameWithMockProviders"
    targets = qsharp.azure.connect(
        f"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}")
    assert isinstance(targets, list)
    assert len(targets) > 0

    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.target()
    assert exception_info.value.error_name == "NoTarget"

    for target in targets:
        active_target = qsharp.azure.target(target.id)
        assert isinstance(active_target, AzureTarget)
        assert active_target == target

    # Submit a snippet operation without parameters
    op = qsharp.compile("""
        operation HelloQ() : Result
        {
            Message($"Hello from quantum world!"); 
            return Zero;
        }
    """)

    job = qsharp.azure.submit(op)
    assert isinstance(job, AzureJob)

    retrieved_job = qsharp.azure.status(job.id)
    assert isinstance(retrieved_job, AzureJob)
    assert job.id == retrieved_job.id

    # Execute a workspace operation with parameters
    op = qsharp.QSharpCallable("Microsoft.Quantum.SanityTests.HelloAgain", None)

    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.execute(op)
    assert exception_info.value.error_name == "JobSubmissionFailed"

    histogram = qsharp.azure.execute(op, count=3, name="test")
    assert isinstance(histogram, dict)

    retrieved_histogram = qsharp.azure.output()
    assert isinstance(retrieved_histogram, dict)
    assert histogram == retrieved_histogram

    # Check that both submitted jobs exist in the workspace
    jobs = qsharp.azure.jobs()
    assert isinstance(jobs, list)
    assert len(jobs) == 2
