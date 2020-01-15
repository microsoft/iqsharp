// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class ConfigMagic : AbstractMagic
    {
        public ConfigMagic(IConfigurationSource configurationSource) : base(
            "config",
            new Documentation {
                Summary = "Allows setting or querying configuration options."
            })
        {
            this.ConfigurationSource = configurationSource;
        }

        public IConfigurationSource ConfigurationSource { get; }

        public override ExecutionResult Run(string? input, IChannel channel)
        {
            // If we didn't get any input, treat it as a query.
            if (input == null || input.Trim().Length == 0)
            {
                var configTable = new Table<KeyValuePair<string, JToken>>
                {
                    Columns = new List<(string, Func<KeyValuePair<string, JToken>, string>)>
                    {
                        ("key", row => row.Key),
                        ("Value", row => JsonConvert.SerializeObject(row.Value))
                    },
                    Rows = ConfigurationSource.Configuration.ToList()
                };
                return configTable.ToExecutionResult();
            }
            else
            {
                // We got an input, so expect it to be of the form
                // <key> = <parsable value>, such as foo = "bar".
                var parts = input.Split("=", 2);
                if (parts.Length != 2)
                {
                    return "Expected config option in the form key = value."
                           .ToExecutionResult(ExecuteStatus.Error);
                }
                var key = parts[0].Trim();
                var value = JToken.Parse(parts[1]);
                ConfigurationSource.Configuration[key] = value;
                // Serialize back to a string for reporting to the user.
                return JsonConvert.SerializeObject(value).ToExecutionResult();
            }
        }
    }
}
