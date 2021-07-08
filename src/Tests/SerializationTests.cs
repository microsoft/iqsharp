// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Tests.IQSharp
{
    public class Complex : UDTBase<(double, double)>
    {
        public Complex((double, double) data) : base(data) { }
    }

    public class QubitState : UDTBase<(Complex, Complex)>
    {
        public QubitState((Complex, Complex) data) : base(data) { }
    }

    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public async Task SerializeFlatUdtInstance()
        {
            var complex = new Complex((12.0, 1.4));
            var token = JToken.Parse(JsonConvert.SerializeObject(complex, JsonConverters.TupleConverters));
            Assert.AreEqual(typeof(Complex).FullName, token?["@type"]?.Value<string>());
            Assert.AreEqual(12, token?["Item1"]?.Value<double>());
            Assert.AreEqual(1.4, token?["Item2"]?.Value<double>());
        }

        [TestMethod]
        public async Task DeserializeFlatUdtInstance()
        {
            var jsonData = @"
                {
                    ""@type"": ""COMPLEX"",
                    ""Item1"": 12.0,
                    ""Item2"": 1.4
                }
            ".Replace("COMPLEX", typeof(Complex).FullName);
            var complex = JsonConvert.DeserializeObject<Complex>(jsonData, JsonConverters.TupleConverters);
            Assert.AreEqual(new Complex((12.0, 1.4)), complex);
        }

        [TestMethod]
        public async Task SerializeNestedUdtInstance()
        {
            var testValue = new QubitState((new Complex((0.1, 0.2)), new Complex((0.3, 0.4))));
            var jsonData = JsonConvert.SerializeObject(testValue, JsonConverters.TupleConverters);
            System.Console.WriteLine(jsonData);
            var token = JToken.Parse(jsonData);
            Assert.AreEqual(typeof(QubitState).FullName, token?["@type"]?.Value<string>());
            Assert.AreEqual(typeof(Complex).FullName, token?["Item1"]?["@type"]?.Value<string>());
            Assert.AreEqual(0.1, token?["Item1"]?["Item1"]?.Value<double>());
            Assert.AreEqual(0.2, token?["Item1"]?["Item2"]?.Value<double>());
            Assert.AreEqual(typeof(Complex).FullName, token?["Item2"]?["@type"]?.Value<string>());
            Assert.AreEqual(0.3, token?["Item2"]?["Item1"]?.Value<double>());
            Assert.AreEqual(0.4, token?["Item2"]?["Item2"]?.Value<double>());
        }

        [TestMethod]
        public async Task DeserializeNestedUdtInstance()
        {
            var jsonData = @"
                {
                    ""@type"": ""STATE"",
                    ""Item1"": {
                        ""@type"": ""COMPLEX"",
                        ""Item1"": 0.1,
                        ""Item2"": 0.2
                    },
                    ""Item2"": {
                        ""@type"": ""COMPLEX"",
                        ""Item1"": 0.3,
                        ""Item2"": 0.4
                    }
                }
            "
            .Replace("COMPLEX", typeof(Complex).FullName)
            .Replace("STATE", typeof(QubitState).FullName);
            var deserialized = JsonConvert.DeserializeObject<QubitState>(jsonData, JsonConverters.TupleConverters);
            Assert.AreEqual(
                new QubitState((new Complex((0.1, 0.2)), new Complex((0.3, 0.4)))),
                deserialized
            );
        }

        [TestMethod]
        public async Task SerializeResultInstance()
        {
            {
                Result result = new ResultConst(ResultValue.One);
                var token = JToken.Parse(JsonConvert.SerializeObject(result, JsonConverters.TupleConverters));
                Assert.AreEqual(1, token.Value<int>());
            }
            {
                Result result = new ResultConst(ResultValue.Zero);
                var token = JToken.Parse(JsonConvert.SerializeObject(result, JsonConverters.TupleConverters));
                Assert.AreEqual(0, token.Value<int>());
            }
        }

    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
