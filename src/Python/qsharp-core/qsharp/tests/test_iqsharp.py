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

