// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     A magic command that sets or queries configuration options.
    /// </summary>
    public class ConfigMagic : AbstractMagic
    {
        /// <summary>
        ///     Constructs a magic command that sets or queries configuration
        ///     options using a given configuration source.
        /// </summary>
        public ConfigMagic(IConfigurationSource configurationSource) : base(
            "config",
            new Documentation {
                Summary = "Allows setting or querying configuration options."
            })
        {
            this.ConfigurationSource = configurationSource;
        }

        /// <summary>
        ///     The configuration source which this magic command queries or
        ///     sets configuration options in.
        /// </summary>
        public IConfigurationSource ConfigurationSource { get; }


        /// <inheritdoc />
        public override async Task<ExecutionResult> Run(string? input, IChannel channel)
        {
            // If we didn't get any input, treat it as a query.
            if (input == null || input.Trim().Length == 0)
            {
                var configTable = new Table<KeyValuePair<string, JToken>>
                {
                    Columns = new List<(string, Func<KeyValuePair<string, JToken>, string>)>
                    {
                        ("Configuration key", row => row.Key),
                        ("Value", row => JsonConvert.SerializeObject(row.Value))
                    },
                    Rows = ConfigurationSource.Configuration.ToList()
                };
                return configTable.ToExecutionResult();
            }
            else if (input.Trim().ToLowerInvariant() == "--save")
            {
                ConfigurationSource.Persist();
                return ExecuteStatus.Ok.ToExecutionResult();
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
