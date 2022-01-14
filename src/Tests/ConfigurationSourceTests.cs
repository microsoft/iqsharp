// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.IQSharp
{
    [TestClass]
    public class ConfigurationSourceTests
    {
        [TestMethod]
        public void Persists()
        {
            static void DeleteConfig()
            {
                if (File.Exists(ConfigurationSource.ConfigPath))
                {
                    File.Delete(ConfigurationSource.ConfigPath);
                }
                Assert.IsFalse(File.Exists(ConfigurationSource.ConfigPath));
            };

            DeleteConfig();

            try
            {
                var config = new ConfigurationSource() as IConfigurationSource;
                Assert.IsNotNull(config);
                Assert.IsFalse(File.Exists(ConfigurationSource.ConfigPath)); // Make sure the file is not created automatically.
                Assert.AreEqual(CommonNativeSimulator.BasisStateLabelingConvention.LittleEndian, config.BasisStateLabelingConvention);
                Assert.AreEqual(false, config.TruncateSmallAmplitudes);
                Assert.AreEqual(4, config.MeasurementDisplayPrecision);
                Assert.AreEqual("mixed", config.ExperimentalSimulatorRepresentation);

                // Change some values
                config.Configuration["dump.basisStateLabelingConvention"] = JToken.Parse("\"BigEndian\"");
                config.Configuration["dump.truncateSmallAmplitudes"] = JToken.Parse("true");
                config.Configuration["dump.measurementDisplayPrecision"] = JToken.Parse("2");
                config.Configuration["experimental.simulators.representation"] = JToken.Parse("\"simple\"");

                // Save:
                config.Persist();
                Assert.IsTrue(File.Exists(ConfigurationSource.ConfigPath));

                config = new ConfigurationSource() as IConfigurationSource;
                Assert.IsNotNull(config);
                Assert.AreEqual(CommonNativeSimulator.BasisStateLabelingConvention.BigEndian, config.BasisStateLabelingConvention);
                Assert.AreEqual(true, config.TruncateSmallAmplitudes);
                Assert.AreEqual(2, config.MeasurementDisplayPrecision);
                Assert.AreEqual("simple", config.ExperimentalSimulatorRepresentation);
                Assert.AreEqual(MeasurementDisplayStyle.BarAndNumber, config.MeasurementDisplayStyle);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ReadFromEnvironment()
        {
            var config = new ConfigurationSource(skipLoading: true) as IConfigurationSource;
            Assert.IsNotNull(config);
            Assert.AreEqual(CommonNativeSimulator.BasisStateLabelingConvention.LittleEndian, config.BasisStateLabelingConvention);
            Assert.AreEqual(false, config.TruncateSmallAmplitudes);
            Assert.AreEqual(4, config.MeasurementDisplayPrecision);
            Assert.AreEqual("mixed", config.ExperimentalSimulatorRepresentation);

            // Set values via environment:
            System.Environment.SetEnvironmentVariable("DUMP_BASISSTATELABELINGCONVENTION", "BigEndian");
            System.Environment.SetEnvironmentVariable("IQSHARP_DUMP_TRUNCATESMALLAMPLITUDES", "true");
            System.Environment.SetEnvironmentVariable("DUMP_MEASUREMENTDISPLAYPRECISION", "2");
            System.Environment.SetEnvironmentVariable("IQSHARP_EXPERIMENTAL_SIMULATORS_REPRESENTATION", "simple");

            // Read values again, environment should be reflected:
            Assert.AreEqual(CommonNativeSimulator.BasisStateLabelingConvention.BigEndian, config.BasisStateLabelingConvention);
            Assert.AreEqual(true, config.TruncateSmallAmplitudes);
            Assert.AreEqual(2, config.MeasurementDisplayPrecision);
            Assert.AreEqual("simple", config.ExperimentalSimulatorRepresentation);
            Assert.AreEqual(MeasurementDisplayStyle.BarAndNumber, config.MeasurementDisplayStyle);

            // Reset environment:
            System.Environment.SetEnvironmentVariable("DUMP_BASISSTATELABELINGCONVENTION", null);
            System.Environment.SetEnvironmentVariable("IQSHARP_DUMP_TRUNCATESMALLAMPLITUDES", null);
            System.Environment.SetEnvironmentVariable("DUMP_MEASUREMENTDISPLAYPRECISION", null);
            System.Environment.SetEnvironmentVariable("IQSHARP_EXPERIMENTAL_SIMULATORS_REPRESENTATION", null);
        }
    }
}
