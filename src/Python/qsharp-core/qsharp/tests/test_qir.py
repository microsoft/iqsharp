##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

import unittest
import qsharp
from qsharp.clients.iqsharp import IQSharpError


class TestQir(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # A Q# callable that should work in all target capabilities
        cls.qsharp_callable_basic = qsharp.compile("""
            open Microsoft.Quantum.Intrinsic;
            operation GenerateRandomBitBasic() : Result {
                use qubit = Qubit();
                H(qubit);
                return M(qubit);
            }
        """)
        # A Q# callable with a conditional branch that is not
        # supported in all target capabilities
        cls.qsharp_callable_advanced = qsharp.compile("""
            open Microsoft.Quantum.Intrinsic;
            operation GenerateRandomBitAdvanced() : Result {
                use qubits = Qubit[2];
                H(qubits[0]);
                let r1 = M(qubits[0]);
                if r1 == One {
                    H(qubits[1]);        
                }
                return M(qubits[1]);
            }
        """)

    def test_as_qir(self):
        qir_text = self.qsharp_callable_basic.as_qir()
        self.assertIn("@ENTRYPOINT__GenerateRandomBitBasic", qir_text)

    def test_as_qir_metadata(self):
        metadata = {"azure.target_id": "rigetti.simulator"}
        qir_text = self.qsharp_callable_basic.as_qir(metadata)
        self.assertIn("@ENTRYPOINT__GenerateRandomBitBasic", qir_text)

        metadata = {"azure.target_id": "rigetti.simulator",
                    "azure.target_capability": "BasicExecution"}
        qir_text = self.qsharp_callable_basic.as_qir(metadata)
        self.assertIn("@ENTRYPOINT__GenerateRandomBitBasic", qir_text)

        metadata = {"azure.target_id": "rigetti.simulator",
                    "azure.target_capability": "FullComputation"}
        qir_text = self.qsharp_callable_advanced.as_qir(metadata)
        self.assertIn("@ENTRYPOINT__GenerateRandomBitAdvanced", qir_text)

    def test_as_qir_kwargs(self):
        qir_text = self.qsharp_callable_basic \
                       .as_qir(target="rigetti.simulator")
        self.assertIn("@ENTRYPOINT__GenerateRandomBitBasic", qir_text)

        qir_text = self.qsharp_callable_basic \
                       .as_qir(target="rigetti.simulator",
                               target_capability="BasicExecution")
        self.assertIn("@ENTRYPOINT__GenerateRandomBitBasic", qir_text)

        qir_text = self.qsharp_callable_advanced \
                       .as_qir(target="rigetti.simulator",
                               target_capability="FullComputation")
        self.assertIn("@ENTRYPOINT__GenerateRandomBitAdvanced", qir_text)

    def test_repr_qir_(self):
        metadata = {"azure.target_id": "rigetti.simulator"}
        qir_bitcode = self.qsharp_callable_basic._repr_qir_(metadata)
        self.assertGreater(len(qir_bitcode), 4)
