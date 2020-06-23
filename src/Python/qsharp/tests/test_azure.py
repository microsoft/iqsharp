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

def test_workspace_create_with_parameters():
    """
    Tests behavior of a mock workspace with providers, using parameters to connect.
    """
    targets = qsharp.azure.connect(
        storage="test",
        subscription="test",
        resourceGroup="test",
        workspace="WorkspaceNameWithMockProviders"
    )
    assert isinstance(targets, list)
    assert len(targets) > 0

    _test_workspace_with_providers_after_connection()

def test_workspace_create_with_resource_id():
    """
    Tests behavior of a mock workspace with providers, using resource ID to connect.
    """
    subscriptionId = "f846b2bd-d0e2-4a1d-8141-4c6944a9d387"
    resourceGroupName = "test"
    workspaceName = "WorkspaceNameWithMockProviders"
    targets = qsharp.azure.connect(
        resourceId=f"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}")
    assert isinstance(targets, list)
    assert len(targets) > 0

    _test_workspace_with_providers_after_connection()
    _test_workspace_job_execution()

def test_workspace_create_with_resource_id_and_storage():
    """
    Tests behavior of a mock workspace with providers, using resource ID and storage connection string to connect.
    """
    subscriptionId = "f846b2bd-d0e2-4a1d-8141-4c6944a9d387"
    resourceGroupName = "test"
    workspaceName = "WorkspaceNameWithMockProviders"
    storageAccountConnectionString = "test"
    targets = qsharp.azure.connect(
        resourceId=f"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}",
        storage=storageAccountConnectionString)
    assert isinstance(targets, list)
    assert len(targets) > 0

    _test_workspace_with_providers_after_connection()

def _test_workspace_with_providers_after_connection():
    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.target()
    assert exception_info.value.error_name == "NoTarget"

    targets = qsharp.azure.connect()
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

def _test_workspace_job_execution():
    # Execute a workspace operation with parameters
    op = qsharp.QSharpCallable("Microsoft.Quantum.SanityTests.HelloAgain", None)

    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.execute(op)
    assert exception_info.value.error_name == "JobSubmissionFailed"

    histogram = qsharp.azure.execute(op, count=3, name="test", timeout=3, poll=0.5)
    assert isinstance(histogram, dict)

    retrieved_histogram = qsharp.azure.output()
    assert isinstance(retrieved_histogram, dict)
    assert histogram == retrieved_histogram

    # Check that both submitted jobs exist in the workspace
    jobs = qsharp.azure.jobs()
    assert isinstance(jobs, list)
    assert len(jobs) == 2

    # Check that job filtering works
    jobs = qsharp.azure.jobs(job.id)
    print(job.id)
    assert isinstance(jobs, list)
    assert len(jobs) == 1
