// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using System.Linq;
using System.Collections.Generic;

namespace Tests.IQSharp
{

    internal static class DisplayExtensions
    {
        internal static IEnumerable<(T, string)> WhereLabelIs<T>(
            this IEnumerable<(T, string)> source,
            string label
        ) =>
            source.Where(
                item =>
                {
                    var (data, currentLabel) = item;
                    return label == currentLabel;
                }
            );
    }

    [TestClass]
    public class DisplayConverterTests
    {
        private readonly CommonNativeSimulator.DisplayableState testState = new CommonNativeSimulator.DisplayableState
        {
            QubitIds = new[] {0, 1, 2},
            NQubits = 3,
            Amplitudes = new Dictionary<System.Numerics.BigInteger, System.Numerics.Complex>(Enumerable
                .Range(0, 8)
                .Select(idx => new KeyValuePair<System.Numerics.BigInteger, System.Numerics.Complex>(
                    new System.Numerics.BigInteger(idx), new System.Numerics.Complex(0, idx)))
            )
        };

        [TestMethod]
        public void TestBigEndianLabels()
        {
            var testItem = testState
                .SignificantAmplitudes(
                    CommonNativeSimulator.BasisStateLabelingConvention.BigEndian,
                    false,
                    0
                )
                .WhereLabelIs("6")
                .Single();
            Assert.AreEqual(testItem.Item1.Imaginary, 3.0);
        }

        [TestMethod]
        public void TestLittleEndianLabels()
        {
            var testItem = testState
                .SignificantAmplitudes(
                    CommonNativeSimulator.BasisStateLabelingConvention.LittleEndian,
                    false,
                    0
                )
                .WhereLabelIs("6")
                .Single();
            Assert.AreEqual(testItem.Item1.Imaginary, 6.0);
        }

        [TestMethod]
        public void TestBitstringLabels()
        {
            var testItem = testState
                .SignificantAmplitudes(
                    CommonNativeSimulator.BasisStateLabelingConvention.Bitstring,
                    false,
                    0
                )
                .WhereLabelIs("011")
                .Single();
            Assert.AreEqual(testItem.Item1.Imaginary, 6.0);
        }

    }

}
