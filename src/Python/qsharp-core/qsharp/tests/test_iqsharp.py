#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_iqsharp.py: Tests basic Q#/Python interop functionality.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

import json
import numpy as np
import os
import pytest
import qsharp
from .utils import set_environment_variables

print ( qsharp.component_versions() )

## SETUP ##

@pytest.fixture(scope="session", autouse=True)
def session_setup():
    set_environment_variables()

try:
    import qutip as qt
except ImportError:
    qt = None

skip_if_no_qutip = pytest.mark.skipif(qt is None, reason="Test requires QuTiP.")

# If we can't import Microsoft as a Python package, then that means that
# the Q# workspace isn't present — probably because the qsharp-core package
# has been installed, since `Operations.qs` isn't part of the package data.
try:
    import Microsoft.Quantum.SanityTests
    workspace_present = True
except ImportError:
    workspace_present = False

skip_if_no_workspace = pytest.mark.skipif(not workspace_present, reason="Local Q# workspace not available in installed package.")

# Finally, not all tests work well from within conda build environments, so
# we disable those tests here.
is_conda = getattr(qsharp.__version__, "is_conda", False) or os.environ.get("QSHARP_PY_ISCONDA", "False").lower() == "true"
skip_if_conda = pytest.mark.skipif(is_conda, reason="Test is not supported from conda-build.")

## TESTS ##

@skip_if_no_workspace
def test_simulate():
    """
    Checks that a simple simulate works correctly, both using callable() and 
    callable.simulate()
    """
    from Microsoft.Quantum.SanityTests import HelloQ, HelloAgain
    assert HelloQ() == HelloQ.simulate() == ()
    assert HelloAgain(
        count=1, name="Ada") == HelloAgain.simulate(count=1, name="Ada")


def test_toffoli_simulate():
    foo = qsharp.compile("""
        open Microsoft.Quantum.Measurement;

        operation Foo() : Result {
            using (q = Qubit()) {
                X(q);
                return MResetZ(q);
            }
        }
    """)
    assert foo.toffoli_simulate() == 1

@skip_if_no_workspace
def test_tuples():
    """
    Checks that tuples are correctly encoded both ways.
    """
    from Microsoft.Quantum.SanityTests import IndexIntoTuple
    r = IndexIntoTuple.simulate(count=2, tuples=[(0, "Zero"), (1, "One"), (0, "Two"), (0, "Three")])
    assert r == (0, "Two")

@skip_if_no_workspace
def test_numpy_types():
    """
    Checks that numpy types are correctly encoded.
    """
    from Microsoft.Quantum.SanityTests import IndexIntoNestedArray, IndexIntoTuple

    r = IndexIntoNestedArray.simulate(index1=1, index2=0, nestedArray=np.array([[100, 101], np.array([110, 111], dtype=np.int64)]))
    assert r == 110
    
    tuples = [(0, "Zero"), (1, "One")]
    tuples_array = np.empty(len(tuples), dtype=object)
    tuples_array[:] = tuples
    r = IndexIntoTuple.simulate(count=1, tuples=tuples_array)
    assert r == (1, "One")

@skip_if_no_workspace
def test_result():
    """
    Checks that Result-type arguments are handled correctly.
    """
    from Microsoft.Quantum.SanityTests import EchoResult
    r = EchoResult.simulate(input=qsharp.Result.One)
    assert r == qsharp.Result.One    

    r = EchoResult.simulate(input=1)
    assert r == qsharp.Result.One

    # Current behavior is that non-integer values will get rounded to
    # the nearest integer then converted to a Result. Once that behavior
    # is fixed, this test should be updated to ensure that the following
    # code throws a qsharp.IQSharpError exception.
    # See https://github.com/microsoft/qsharp-runtime/issues/376.
    r = EchoResult.simulate(input=0.2)
    assert r == qsharp.Result.Zero

@skip_if_no_workspace
def test_long_tuple():
    """
    Checks that a 10-tuple argument and return value are handled correctly.
    """
    ten_tuple = (0, 10, 20, 30, 40, 50, 60, 70, 80, 90)
    
    from Microsoft.Quantum.SanityTests import IndexIntoTenTuple
    r = IndexIntoTenTuple.simulate(index=3, tenTuple=ten_tuple)
    assert r == 30
    r = IndexIntoTenTuple.simulate(index=8, tenTuple=ten_tuple)
    assert r == 80
    
    from Microsoft.Quantum.SanityTests import EchoTenTuple
    r = EchoTenTuple.simulate(tenTuple=ten_tuple)
    assert r == ten_tuple

@skip_if_no_workspace
def test_paulis():
    """
    Checks that Pauli-type and arrays of Pauli-type arguments are handled correctly.
    """
    from Microsoft.Quantum.SanityTests import SwapFirstPauli

    paulis = [qsharp.Pauli.Z, qsharp.Pauli.Z, qsharp.Pauli.Z]
    pauliToSwap = qsharp.Pauli.X
    r = SwapFirstPauli.simulate(paulis=paulis, pauliToSwap=pauliToSwap)

    assert r[0] == [qsharp.Pauli.X, qsharp.Pauli.Z, qsharp.Pauli.Z]
    assert r[1] == qsharp.Pauli.Z

    # Should also work with string representation
    paulis = ["PauliZ", "PauliZ", "PauliZ"]
    pauliToSwap = "PauliX"
    r = SwapFirstPauli.simulate(paulis=paulis, pauliToSwap=pauliToSwap)

    assert r[0] == [qsharp.Pauli.X, qsharp.Pauli.Z, qsharp.Pauli.Z]
    assert r[1] == qsharp.Pauli.Z

@skip_if_no_workspace
def test_estimate():
    """
    Verifies that resource estimation works.
    """
    from Microsoft.Quantum.SanityTests import HelloAgain
    r = HelloAgain.estimate_resources(count=4, name="estimate test")
    assert r['Measure'] == 8
    assert r['QubitClifford'] == 1
    assert r['BorrowedWidth'] == 0

@skip_if_no_workspace
def test_trace():
    """
    Verifies the trace commands works.
    """
    from Microsoft.Quantum.SanityTests import HelloAgain
    r = HelloAgain.trace(count=1, name="trace test")
    print(r)
    assert len(r['qubits']) == 1
    assert len(r['operations']) == 1
    assert len(r['operations'][0]['children']) == 2
    assert r['operations'][0]['gate'] == 'HelloAgain'
    assert r['operations'][0]['children'][0]['gate'] == 'M'
    assert r['operations'][0]['children'][1]['gate'] == 'Reset'


def test_simple_compile():
    """
    Verifies that compile works
    """
    op = qsharp.compile( """
    operation HelloQ() : Result
    {
        Message($"Hello from quantum world!"); 
        return Zero;
    }
""")
    r = op.simulate()
    assert r == 0
    

def test_multi_compile():
    """
    Verifies that compile works and that operations
    are returned in the correct order
    """
    ops = qsharp.compile( """
    operation HelloQ() : Result
    {
        Message($"Hello from quantum world!"); 
        return One;
    }

    operation Hello2() : Result
    {
        Message($"Will call hello."); 
        return HelloQ();
    }
""")
    assert "HelloQ" == ops[0]._name
    assert "Hello2" == ops[1]._name

    r = ops[1].simulate()
    assert r == qsharp.Result.One


def test_config():
    """
    Verifies get and set of config settings of various types
    """
    qsharp.config["dump.basisStateLabelingConvention"] = "Bitstring"
    qsharp.config["dump.truncateSmallAmplitudes"] = True
    qsharp.config["dump.truncationThreshold"] = 1e-6

    assert qsharp.config["dump.basisStateLabelingConvention"] == "Bitstring"
    assert qsharp.config["dump.truncateSmallAmplitudes"] == True
    assert qsharp.config["dump.truncationThreshold"] == 1e-6

@skip_if_conda
def test_packages():
    """
    Verifies default package command
    """
    pkg_count = len(qsharp.packages._client.get_packages())

    with pytest.raises(Exception):
        qsharp.packages.add('Invalid.Package.!!!!!!')
    assert pkg_count == len(qsharp.packages._client.get_packages())

    # We test using a non-QDK package name to avoid possible version conflicts.
    qsharp.packages.add('Microsoft.Extensions.Logging')
    assert (pkg_count+1) == len(qsharp.packages._client.get_packages())


def test_projects(tmp_path):
    """
    Verifies default project command
    """
    assert 0 == len(qsharp.projects._client.get_projects())

    with pytest.raises(Exception):
        qsharp.projects.add('../InvalidPath/InvalidProject.txt')
    assert 0 == len(qsharp.projects._client.get_projects())

    temp_project_path = tmp_path / 'temp_iqsharp_pytest_project.csproj'
    temp_project_path.write_text(f'''
        <Project Sdk="Microsoft.Quantum.Sdk/0.12.20072031">
            <PropertyGroup>
                <TargetFramework>netstandard2.1</TargetFramework>
                <IncludeQsharpCorePackages>false</IncludeQsharpCorePackages>
            </PropertyGroup>
        </Project>
    ''')

    qsharp.projects.add(str(temp_project_path))
    assert 1 == len(qsharp.projects._client.get_projects())

class TestCaptureDiagnostics:
    def test_basic_capture(self):
        dump_plus = qsharp.compile("""
            open Microsoft.Quantum.Diagnostics;

            operation DumpPlus() : Unit {
                use qs = Qubit[2];
                within {
                    H(qs[0]);
                    H(qs[1]);
                } apply {
                    DumpMachine();
                }
            }
        """)

        with qsharp.capture_diagnostics(as_qobj=False) as captured:
            dump_plus.simulate()

        assert 1 == len(captured)
        expected = """
            {
                "diagnostic_kind": "state-vector",
                "qubit_ids": [0, 1],
                "n_qubits": 2,
                "amplitudes": {"0": {"Real": 0.5000000000000001, "Imaginary": 0.0, "Magnitude": 0.5000000000000001, "Phase": 0.0}, "1": {"Real": 0.5000000000000001, "Imaginary": 0.0, "Magnitude": 0.5000000000000001, "Phase": 0.0}, "2": {"Real": 0.5000000000000001, "Imaginary": 0.0, "Magnitude": 0.5000000000000001, "Phase": 0.0}, "3": {"Real": 0.5000000000000001, "Imaginary": 0.0, "Magnitude": 0.5000000000000001, "Phase": 0.0}}
            }
        """
        assert json.dumps(json.loads(expected)) == json.dumps(captured[0])


    @skip_if_no_qutip
    def test_capture_diagnostics_as_qobj(self):
        dump_plus = qsharp.compile("""
            open Microsoft.Quantum.Diagnostics;

            operation DumpPlus() : Unit {
                use qs = Qubit[2];
                within {
                    H(qs[0]);
                    H(qs[1]);
                } apply {
                    DumpMachine();
                }
            }
        """)

        with qsharp.capture_diagnostics(as_qobj=True) as captured:
            dump_plus.simulate()

        assert 1 == len(captured)
        assert abs(1.0 - captured[0].norm()) <= 1e-8

        import qutip as qt
        expected = qt.Qobj([[1], [1], [1], [1]], dims=[[2, 2], [1, 1]]).unit()
        assert (expected - captured[0]).norm() <= 1e-8


    @skip_if_no_qutip
    def test_capture_experimental_diagnostics_as_qobj(self):
        dump_plus = qsharp.compile("""
            open Microsoft.Quantum.Diagnostics;

            operation DumpPlus() : Unit {
                use qs = Qubit[2];
                within {
                    H(qs[0]);
                    H(qs[1]);
                } apply {
                    DumpMachine();
                }
            }
        """)

        qsharp.set_noise_model_by_name('ideal')
        qsharp.config['experimental.simulators.nQubits'] = 2
        qsharp.config['experimental.simulators.representation'] = 'mixed'

        with qsharp.capture_diagnostics(as_qobj=True) as captured:
            dump_plus.simulate_noise()

        assert 1 == len(captured)
        assert abs(1.0 - captured[0].tr()) <= 1e-8

        import qutip as qt
        expected = qt.Qobj([[1], [1], [1], [1]], dims=[[2, 2], [1, 1]]).unit()
        expected = expected * expected.dag()
        assert (expected - captured[0]).norm() <= 1e-8
