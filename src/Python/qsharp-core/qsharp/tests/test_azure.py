#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_azure.py: Tests Azure Quantum functionality against a mock workspace.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

import pytest
import qsharp
from qsharp.azure import AzureError, AzureJob, AzureTarget
from .utils import set_environment_variables

## SETUP ##

@pytest.fixture(scope="session", autouse=True)
def session_setup():
    set_environment_variables()

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
        workspace="test",
        location="test"
    )
    assert targets == []

    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.target("invalid.target")
    assert exception_info.value.error_name == "InvalidTarget"

    jobs = qsharp.azure.jobs()
    assert jobs == []

def test_workspace_create_with_no_location():
    """
    Tests behavior of a mock workspace with no location.
    """
    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.connect(
            storage="test",
            subscription="test",
            resourceGroup="test",
            workspace="test"
        )
    assert exception_info.value.error_name == "NoWorkspaceLocation"

def test_workspace_create_with_parameters():
    """
    Tests behavior of a mock workspace with providers, using parameters to connect.
    """
    targets = qsharp.azure.connect(
        storage="test",
        subscription="test",
        resourceGroup="test",
        workspace="WorkspaceNameWithMockProviders",
        location="test"
    )
    assert isinstance(targets, list)
    assert len(targets) > 0

    _test_workspace_with_providers_after_connection()

def test_workspace_create_with_resource_id():
    """
    Tests behavior of a mock workspace with providers, using resource ID to connect.
    Also verifies case-insensitivity of resource ID parsing.
    """
    subscriptionId = "f846b2bd-d0e2-4a1d-8141-4c6944a9d387"
    resourceGroupName = "test"
    workspaceName = "WorkspaceNameWithMockProviders"
    location = "test"
    targets = qsharp.azure.connect(
        resourceId=f"/subscriptions/{subscriptionId}/RESOurceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}",
        location=location)
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
    location = "test"
    targets = qsharp.azure.connect(
        resourceId=f"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Quantum/Workspaces/{workspaceName}",
        storage=storageAccountConnectionString,
        location=location)
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

    # Submit a job with the "jobParams" parameter
    job2 = qsharp.azure.submit(op, jobParams={"key1": "value1", "key2": "value2"})
    assert isinstance(job2, AzureJob)

def _test_workspace_job_execution():
    # Execute a workspace operation with parameters
    op = qsharp.QSharpCallable("Microsoft.Quantum.SanityTests.HelloAgain", None)

    with pytest.raises(AzureError) as exception_info:
        qsharp.azure.execute(op)
    assert exception_info.value.error_name == "JobSubmissionFailed"

    histogram = qsharp.azure.execute(op, count=3, name="test", timeout=3, poll=2)
    assert isinstance(histogram, dict)

    retrieved_histogram = qsharp.azure.output()
    assert isinstance(retrieved_histogram, dict)
    assert histogram == retrieved_histogram

    # Check that the submitted job exists in the workspace
    jobs = qsharp.azure.jobs()
    assert isinstance(jobs, list)
    assert len(jobs) == 1

    # Check that job filtering works
    jobs = qsharp.azure.jobs(jobs[0].id)
    assert isinstance(jobs, list)
    assert len(jobs) == 1
    
    # Check that job count works
    jobs = qsharp.azure.jobs(count=1)
    assert isinstance(jobs, list)
    assert len(jobs) == 1

    jobs = qsharp.azure.jobs("invalid", count=10)
    assert isinstance(jobs, list)
    assert len(jobs) == 0

    # Execute a job with the "jobParams" parameter
    histogram2 = qsharp.azure.execute(op, count=3, name="test2", timeout=3, poll=2, jobParams={"key": "value"})
    assert isinstance(histogram2, dict)
