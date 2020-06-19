// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Kernel
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
                Summary = "Allows setting or querying configuration options.",
                Description = @"
                    This magic command allows for setting or querying
                    configuration options used to control the behavior of the
                    IQ# kernel (e.g.: state visualization options), and to
                    save those options to a JSON file in the current working
                    directory.
                ".Dedent(),
                Examples = new []
                {
                    @"
                        Print a list of all currently set configuration options:
                        ```
                        In []: %config
                        Out[]: Configuration key                 Value
                               --------------------------------- -----------
                               dump.basisStateLabelingConvention ""BigEndian""
                               dump.truncateSmallAmplitudes      true
                        ```
                    ",

                    @"
                        Configure the `DumpMachine` and `DumpRegister` callables
                        to use big-endian convention:
                        ```
                        In []: %config dump.basisStateLabelingConvention = ""BigEndian""
                        Out[]: ""BigEndian""
                        ```
                    ".Dedent(),

                    @"
                        Save current configuration options to `.iqsharp-config.json`
                        in the current working directory:
                        ```
                        In []: %config --save
                        Out[]: 
                        ```
                        Note that options saved this way will be applied automatically
                        the next time a notebook in the current working
                        directory is loaded.
                    ".Dedent()
                }
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
        public override ExecutionResult Run(string? input, IChannel channel, CancellationToken cancellationToken)
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
