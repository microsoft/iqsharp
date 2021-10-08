#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_live.py: Tests Azure Quantum functionality Live.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

import pytest
import warnings

## TESTS ##

def connect():
    import qsharp.azure

    return qsharp.azure.connect(
        credential="environment"
    )


def has_completed(job) -> bool:
    """Check if the job has completed."""
    return (
        job.status == "Succeeded"
        or job.status == "Failed"
        or job.status == "Cancelled"
    )

def wait_until_completed(job):
    import time
    import qsharp.azure

    max_poll_wait_secs = 5
    timeout_secs = 30
    poll_wait = 0.2
    total_time = 0.

    while not has_completed(job):
        if total_time >= timeout_secs:
            raise TimeoutError(f"The wait time has exceeded {timeout_secs} seconds.")

        time.sleep(poll_wait)
        total_time += poll_wait
        job = qsharp.azure.status(job.id)
        poll_wait = (
            max_poll_wait_secs
            if poll_wait >= max_poll_wait_secs
            else poll_wait * 1.5
        )

def test_ionq_targets():
    """
    Tests that we can fetch targets from the service,
    and that the workspace includes the targets we need for submission
    """
    targets = connect()
    assert len(targets) > 2

    target_ids = [t.id for t in targets]
    assert 'ionq.simulator' in target_ids
    assert 'ionq.qpu' in target_ids

def test_ionq_submit():
    """
    Test that the SampleQrng operation can be submitted successfully on the ionq.simulator
    """
    import time
    import qsharp
    from Microsoft.Quantum.Tests import SampleQrng

    # Make sure we can simulate locally:
    count = 3
    result = SampleQrng.simulate(count=count, name='andres')
    assert len(result) == count

    import qsharp.azure
    connect()

    t = qsharp.azure.target("ionq.simulator")
    assert isinstance(t, qsharp.azure.AzureTarget)
    assert t.id == "ionq.simulator"

    job = qsharp.azure.submit(SampleQrng, count=count, name="andres")
    assert isinstance(job, qsharp.azure.AzureJob)
    assert not job.id == ''
    print("Submitted job: ", job.id)

    try:
        wait_until_completed(job)
    except TimeoutError:
        warnings.warn("IonQ execution exceeded timeout. Skipping fetching results.")
    else:
        job = qsharp.azure.status()
        assert isinstance(job, qsharp.azure.AzureJob)
        assert job.status == "Succeeded"

        histogram = {
            '[0,0,0]': 0.125,
            '[0,0,1]': 0.125,
            '[0,1,0]': 0.125,
            '[0,1,1]': 0.125,
            '[1,0,0]': 0.125,
            '[1,0,1]': 0.125,
            '[1,1,0]': 0.125,
            '[1,1,1]': 0.125
        }
        retrieved_histogram = qsharp.azure.output()
        assert isinstance(retrieved_histogram, dict)
        assert histogram == retrieved_histogram

def test_honeywell_targets():
    """
    Tests that we can fetch targets from the service,
    and that the workspace includes the targets we need for submission
    """
    targets = connect()
    assert len(targets) > 2

    target_ids = [t.id for t in targets]
    assert 'honeywell.hqs-lt-s1' in target_ids
    assert 'honeywell.hqs-lt-s1-apival' in target_ids

def test_honeywell_submit():
    """
    Test that the RunTeleport operation can be submitted successfully on the honeywell apival target
    """
    import qsharp
    from Microsoft.Quantum.Tests import RunTeleport

    # Make sure we can simulate locally:
    expected = True
    result = RunTeleport.simulate(doPlus=expected)
    assert result == 0 if expected else 1

    import qsharp.azure
    connect()

    t = qsharp.azure.target("honeywell.hqs-lt-s1-apival")
    assert isinstance(t, qsharp.azure.AzureTarget)
    assert t.id == "honeywell.hqs-lt-s1-apival"

    job = qsharp.azure.submit(RunTeleport, doPlus=expected)
    assert isinstance(job, qsharp.azure.AzureJob)
    assert not job.id == ''
    print("Submitted job: ", job.id)

    try: 
        wait_until_completed(job)
    except TimeoutError:
        warnings.warn("Honeywell execution exceeded timeout. Skipping fetching results.")
    else:
        job = qsharp.azure.status()
        assert isinstance(job, qsharp.azure.AzureJob)
        if job.status == "Succeeded":
            retrieved_histogram = qsharp.azure.output()
            assert isinstance(retrieved_histogram, dict)
            assert '0' in retrieved_histogram
    