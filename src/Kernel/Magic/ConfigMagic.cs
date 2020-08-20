// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

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
            new Documentation
            {
                Summary = "Allows setting or querying configuration options.",
                Description = @"
                    This magic command allows for setting or querying
                    configuration options used to control the behavior of the
                    IQ# kernel (such as state visualization options). It also
                    allows for saving those options to a JSON file in the current
                    working directory (using the `--save` option).

                    #### Configuration settings

                    **`dump.basisStateLabelingConvention`**

                    **Value:** `""LittleEndian""` (default), `""BigEndian""`, or `""Bitstring""`

                    The convention to be used when labeling computational
                    basis states in output from callables such as `DumpMachine` or `DumpRegister`.

                    **`dump.truncateSmallAmplitudes`**

                    **Value:** `true` or `false` (default)

                    Hides basis states of a state vector whose measurement probabilities
                    (i.e., squared amplitudes) are smaller than a particular threshold, as determined by
                    the `dump.truncationThreshold` setting.

                    **`dump.truncationThreshold`**

                    **Value:** floating point number such as `0.001` or `1E-8` (default `1E-10`)

                    If `dump.truncateSmallAmplitudes` is set to `true`, determines the
                    threshold for measurement probabilities (i.e., squared amplitudes) below which to hide the display
                    of basis states of a state vector.

                    **`dump.phaseDisplayStyle`**

                    **Value:** `""ArrowOnly""` (default), `""NumberOnly""`, `""ArrowAndNumber""`, or `""None""`

                    Configures the phase visualization style in output from callables such as
                    `DumpMachine` or `DumpRegister`. Supports displaying phase as arrows, numbers (in radians), both, or neither.

                    **`dump.measurementDisplayStyle`**

                    **Value:** '""NumberOnly""' , `""BarOnly""`, `""BarAndNumber""` (default), or `""None""`

                    Configures the measurement probability visualization style in output of callables such as 
                    `DumpMachine` or `DumpRegister`. Supports displaying measurement probability as progress bars, numbers, both,
                    or neither. 

                    **'dump.measurementDisplayPrecision'**

                    **Value:** non-negative integer such as '1' or '2' (default '4')

                    Sets the precision of the measurement probability represented as a percentage when set to 'NumberOnly' or
                    'BarAndNumber'.

                    **`dump.measurementDisplayHistogram`**

                    **Value:** `true` (default) or `false'

                    If `dump.measurementDisplayHistogram` is set to `true`, displays the
                    histogram representation of the state of the simulator underneath the original chart output.


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
                    ".Dedent(),

                    @"
                        Configure the `DumpMachine` and `DumpRegister` callables
                        to use big-endian convention:
                        ```
                        In []: %config dump.basisStateLabelingConvention=""BigEndian""
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
        public override ExecutionResult Run(string? input, IChannel channel)
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
                    return "Expected config option in the form key=value."
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
