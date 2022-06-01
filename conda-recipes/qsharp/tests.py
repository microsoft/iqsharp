# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
logging.basicConfig(level=logging.DEBUG)

# Make sure to setup QuTiP as soon as possible, so as to avoid circular
# dependencies. If QuTiP is not available, future import calls in forwarded
# tests will fail with a simple ImportError.
try:
    import qutip as _qt
    logging.info("Successfully imported QuTiP.")
except ImportError as ex:
    logging.error(f"Failed to import QuTiP with error \"{ex.msg}\". Skipping QuTiP-based tests.", exc_info=ex)

import os

import pytest
os.environ["QSHARP_PY_ISCONDA"] = "True"

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
