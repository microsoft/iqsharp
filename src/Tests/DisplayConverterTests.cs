// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Quantum.IQSharp.Jupyter;
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
        private readonly DisplayableState testState = new DisplayableState(
            QubitIds: new[] {0, 1, 2},
            NQubits: 3,
            Amplitudes: Enumerable
                .Range(0, 8)
                .Select(idx => new System.Numerics.Complex(0, idx))
                .ToArray(),
            DivId: "test-state"
        );

        [TestMethod]
        public void TestBigEndianLabels()
        {
            var testItem = testState
                .SignificantAmplitudes(
                    BasisStateLabelingConvention.BigEndian,
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
                    BasisStateLabelingConvention.LittleEndian,
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
                    BasisStateLabelingConvention.Bitstring,
                    false,
                    0
                )
                .WhereLabelIs("011")
                .Single();
            Assert.AreEqual(testItem.Item1.Imaginary, 6.0);
        }

    }

}
