import qsharp

def test_simple_compile():
    """
    Verifies that compile works
    """
    op = qsharp.compile( """
    operation HelloQ() : Result
    {
        Message($"Hello from quantum world!"); 
        return One;
    }
""")
    r = op.simulate()
    assert r == qsharp.Result.One
    