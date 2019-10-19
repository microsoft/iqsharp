using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class SettingsTests
    {
        public SettingsMagic Init(string workspace = "Workspace")
        {
            return Startup.Create<SettingsMagic>(workspace);
        }


        [TestMethod]
        public void SettingSettings()
        {
            var count = 0;
            IRuntimeSettings settings = new RuntimeSettings(null);
            Assert.AreEqual(0, settings.All.Count());

            settings.SettingSet += (_,__) => count++;

            settings["foo"] = "bar";
            settings["xyz"] = "alpha";

            Assert.AreEqual(2, settings.All.Count());
            Assert.AreEqual(2, count);
            Assert.AreEqual("bar", settings["foo"]);

            settings["foo"] = "andres";
            Assert.AreEqual(2, settings.All.Count());
            Assert.AreEqual(3, count);
            Assert.AreEqual("andres", settings["foo"]);
        }

        [TestMethod]
        public void DefaultSettings()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>() 
                {
                    { "foo", "a;b;c;d" },
                    { "bar", null }
                })
                .Build();

            IRuntimeSettings settings = new RuntimeSettings(config);
            Assert.AreEqual(2, settings.All.Count());
            Assert.AreEqual("a;b;c;d", settings["foo"]);
            Assert.AreEqual(null, settings["bar"]);
            Assert.AreEqual(null, settings["invalid"]);
        }

        [TestMethod]
        public void AllSettingsMagic()
        {
            var settingsMagic = Init();
            var channel = new MockChannel();
            var response = settingsMagic.Execute(" ", channel);
            IQSharpEngineTests.PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            var result = (response.Output as IEnumerable<(string, string)>).ToDictionary(s => s.Item1, s => s.Item2);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(Path.GetFullPath("Workspace"), result["Workspace"]);
        }

        [TestMethod]
        public void ModifySettingsMagic()
        {
            var settingsMagic = Init();
            var channel = new MockChannel();
            var response = settingsMagic.Execute(" ", channel);
            IQSharpEngineTests.PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            var result = (response.Output as IEnumerable<(string, string)>).ToDictionary(s => s.Item1, s => s.Item2);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(Path.GetFullPath("Workspace"), result["Workspace"]);
            Assert.IsNotNull(result["DefaultPackageVersions"]);

            response = settingsMagic.Execute(@"
andres=1;2;3

#this is just a comment:
Workspace=c:\my\location

 # a setting with blank spaces, 
 # notice everything gets trimmed on the key but not the value:
  trimmed  = whatever  

", channel);
            result = (response.Output as IEnumerable<(string, string)>).ToDictionary(s => s.Item1, s => s.Item2);

            Assert.AreEqual(4, result.Count());
            Assert.AreEqual(@"c:\my\location", result["Workspace"]);
            Assert.AreEqual("1;2;3", result["andres"]);
            Assert.AreEqual(" whatever  ", result["trimmed"]);
            Assert.IsNotNull(result["DefaultPackageVersions"]);
        }
    }
}
