# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging

logging.basicConfig(level=logging.DEBUG)

import os

import pytest
os.environ["QSHARP_PY_ISCONDA"] = "True"

from importlib import import_module
from attr import attr
import qsharp

# We also want to make sure that QuTiP is enabled for tests when running via
# conda, even though these tests are optional for the qsharp wheel.
# We use try_import_qutip to force a successful import to populate the same
# cache of the import call used later by tests.
from qsharp.utils import try_import_qutip
try_import_qutip(optional=False)

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

def test_user_agent_extra():
    """
    Verifies that the extra information sent with the user agent for this
    package correctly marks that the package was installed with conda.
    """
    import qsharp.version
    assert getattr(qsharp.version, "is_conda", False)
    assert qsharp.version._user_agent_extra == f"[{qsharp.__version__}](qsharp:conda)"

# Forward tests from the unit testing modules.
def _forward_tests(module_name) -> None:
    logging.debug(f"Importing module {module_name} to forward tests...")
    module = import_module(module_name)

    for attr_name in dir(module):
        if attr_name.startswith("test_") or attr_name.startswith("Test"):
            logging.debug(f"Forwarding {attr_name} from {module_name}.")
            globals()[attr_name] = getattr(module, attr_name)

_forward_tests("qsharp.tests.test_iqsharp")
_forward_tests("qsharp.tests.test_serialization")
