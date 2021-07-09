# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from importlib import import_module
from attr import attr
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

# Forward tests from the unit testing modules.
def _forward_tests(module_name) -> None:
    module = import_module(module_name)

    for attr_name in dir(module):
        if attr_name.startswith("test_") or attr_name.startswith("Test"):
            print(f"Forwarding {attr_name} from {module_name}.")
            globals()[attr_name] = getattr(module, attr_name)

_forward_tests("qsharp.tests.test_iqsharp")
_forward_tests("qsharp.tests.test_serialization")
