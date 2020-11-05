#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_iqsharp.py: Tests basic Q#/Python interop functionality.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

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

## TESTS ##

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


def test_tuples():
    """
    Checks that tuples are correctly encoded both ways.
    """
    from Microsoft.Quantum.SanityTests import IndexIntoTuple
    r = IndexIntoTuple.simulate(count=2, tuples=[(0, "Zero"), (1, "One"), (0, "Two"), (0, "Three")])
    assert r == (0, "Two")


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


def test_estimate():
    """
    Verifies that resource estimation works.
    """
    from Microsoft.Quantum.SanityTests import HelloAgain
    r = HelloAgain.estimate_resources(count=4, name="estimate test")
    assert r['Measure'] == 8
    assert r['QubitClifford'] == 1
    assert r['BorrowedWidth'] == 0


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
        <Project Sdk="Microsoft.Quantum.Sdk/0.13.201118141-beta">
            <PropertyGroup>
                <TargetFramework>netstandard2.1</TargetFramework>
                <IncludeQsharpCorePackages>false</IncludeQsharpCorePackages>
            </PropertyGroup>
        </Project>
    ''')

    qsharp.projects.add(str(temp_project_path))
    assert 1 == len(qsharp.projects._client.get_projects())
