import os
import pytest
import qsharp

print ( qsharp.component_versions() )

def test_simulate():
    """
    Checks that a simple simulate works correctly
    """
    from Microsoft.Quantum.SanityTests import HelloQ
    r = HelloQ.simulate()
    assert r == ()


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
    from Microsoft.Quantum.SanityTests import HelloTuple
    r = HelloTuple.simulate(count=2, tuples=[(0, "Zero"), (1, "One"), (0, "Two"), (0, "Three")])
    assert r == (0, "Two")

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


def test_packages():
    """
    Verifies default package command
    """
    pkg_count = len(qsharp.packages._client.get_packages())

    with pytest.raises(Exception):
        qsharp.packages.add('Invalid.Package.!!!!!!')
    assert pkg_count == len(qsharp.packages._client.get_packages())

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
