#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_live.py: Tests Azure Quantum functionality Live.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

from os import path
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
    timeout_secs = 60
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

@pytest.fixture(scope="session")
def ionq_project():
    import qsharp
    return qsharp.projects.add(path.join(path.dirname(__file__), "qsharp", "ionq", "IonQ.csproj"))

@pytest.mark.usefixtures("ionq_project")
class TestIonQ:
    def test_ionq_targets(self):
        """
        Tests that we can fetch targets from the service,
        and that the workspace includes the targets we need for submission
        """
        targets = connect()
        assert len(targets) > 2

        target_ids = [t.id for t in targets]
        assert 'ionq.simulator' in target_ids
        assert 'ionq.qpu' in target_ids
        assert 'ionq.qpu.aria-1' in target_ids

    def test_ionq_submit(self):
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

@pytest.fixture(scope="session")
def quantinuum_project():
    import qsharp
    return qsharp.projects.add(path.join(path.dirname(__file__), "qsharp", "quantinuum", "Quantinuum.csproj"))

@pytest.mark.usefixtures("quantinuum_project")
class TestQuantinuum:
    def test_quantinuum_targets(self):
        """
        Tests that we can fetch targets from the service,
        and that the workspace includes the targets we need for submission
        """
        targets = connect()
        assert len(targets) > 2

        target_ids = [t.id for t in targets]
        assert 'quantinuum.hqs-lt-s1' in target_ids
        assert 'quantinuum.hqs-lt-s1-apival' in target_ids
        assert 'quantinuum.qpu.h1-1' in target_ids
        assert 'quantinuum.sim.h1-1sc' in target_ids

    # Do not add "True" until QIR is supported for Quantinuum targets.
    @pytest.mark.parametrize("enable_qir", [False])
    def test_quantinuum_submit(self, enable_qir):
        """
        Test that the RunTeleport operation can be submitted successfully on the quantinuum apival target
        """
        import qsharp
        from Microsoft.Quantum.Tests import (
            # A version we can use without having to pass mutable parameters.
            RunTeleportWithPlus
        )

        # Make sure we can simulate locally:
        result = RunTeleportWithPlus.simulate()
        assert result == 0

        import qsharp.azure
        connect()

        t = qsharp.azure.target("quantinuum.hqs-lt-s1-apival")
        assert isinstance(t, qsharp.azure.AzureTarget)
        assert t.id == "quantinuum.hqs-lt-s1-apival"

        t = qsharp.azure.target("quantinuum.sim.h1-1sc")
        assert isinstance(t, qsharp.azure.AzureTarget)
        assert t.id == "quantinuum.sim.h1-1sc"

        if enable_qir:
            qsharp.azure.target_capability("AdaptiveExecution")

        job = qsharp.azure.submit(RunTeleportWithPlus)
        assert isinstance(job, qsharp.azure.AzureJob)
        assert not job.id == ''
        print("Submitted job: ", job.id)

        try: 
            wait_until_completed(job)
        except TimeoutError:
            warnings.warn("Quantinuum execution exceeded timeout. Skipping fetching results.")
        else:
            job = qsharp.azure.status()
            assert isinstance(job, qsharp.azure.AzureJob)
            assert job.status == "Succeeded"
            retrieved_histogram = qsharp.azure.output()
            assert isinstance(retrieved_histogram, dict)
            assert '0' in retrieved_histogram

@pytest.fixture(scope="session")
def estimator_project():
    import qsharp
    return qsharp.projects.add(path.join(path.dirname(__file__), "qsharp", "estimator", "Estimator.csproj"))

@pytest.mark.usefixtures("estimator_project")
class TestEstimator:
    def test_estimator_target(self):
        """
        Tests that we can fetch targets from the service,
        and that the workspace includes the targets we need for submission
        """
        targets = connect()
        assert len(targets) > 2

        target_ids = [t.id for t in targets]
        print(target_ids)
        assert 'microsoft.estimator' in target_ids

    def test_estimator_submit(self):
        """
        Test that the EstimateMultiplication operation can be submitted successfully on microsoft.estimator
        """
        import time
        import qsharp
        from Microsoft.Quantum.Tests import EstimateMultiplication

        import qsharp.azure
        connect()

        t = qsharp.azure.target("microsoft.estimator")
        assert isinstance(t, qsharp.azure.AzureTarget)
        assert t.id == "microsoft.estimator"

        job = qsharp.azure.submit(EstimateMultiplication, bitwidth=4)
        assert isinstance(job, qsharp.azure.AzureJob)
        assert not job.id == ''
        print("Submitted job: ", job.id)

        try:
            wait_until_completed(job)
        except TimeoutError:
            warnings.warn("Resource estimator execution exceeded timeout. Skipping fetching results.")
        else:
            job = qsharp.azure.status()
            assert isinstance(job, qsharp.azure.AzureJob)
            assert job.status == "Succeeded"

            retrieved_output = qsharp.azure.output()
            assert isinstance(retrieved_output, qsharp.results.resource_estimator.ResourceEstimatorResult)

    def test_estimator_submit_items(self):
        """
        Test that the EstimateMultiplication operation can be submitted successfully on microsoft.estimator
        """
        import time
        import qsharp
        from Microsoft.Quantum.Tests import EstimateMultiplication

        import qsharp.azure
        connect()

        t = qsharp.azure.target("microsoft.estimator")
        assert isinstance(t, qsharp.azure.AzureTarget)
        assert t.id == "microsoft.estimator"

        item1 = {"arguments": [{"name": "bitwidth", "value": 4, "type": "Int"}]}
        item2 = {"arguments": [{"name": "bitwidth", "value": 8, "type": "Int"}]}
        job = qsharp.azure.submit(EstimateMultiplication, jobParams={"items": [item1, item2]})
        assert isinstance(job, qsharp.azure.AzureJob)
        assert not job.id == ''
        print("Submitted job: ", job.id)

        try:
            wait_until_completed(job)
        except TimeoutError:
            warnings.warn("Resource estimator execution exceeded timeout. Skipping fetching results.")
        else:
            job = qsharp.azure.status()
            assert isinstance(job, qsharp.azure.AzureJob)
            assert job.status == "Succeeded"

            retrieved_output = qsharp.azure.output()
            assert isinstance(retrieved_output, qsharp.results.resource_estimator.ResourceEstimatorBatchResult)
            assert len(retrieved_output) == 2

            assert retrieved_output[0]["physicalCounts"]["physicalQubits"] == 102094
            assert retrieved_output[1]["physicalCounts"]["physicalQubits"] == 175934
