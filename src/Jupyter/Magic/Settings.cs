// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class SettingsMagic : AbstractMagic
    {
        public SettingsMagic(IRuntimeSettings settings) : base(
            "settings", 
            new Documentation {
                Summary = "Allows to users to control runtime settings from jupyter."
            })
        {
            this.Settings = settings;
        }

        public IRuntimeSettings Settings { get; }

        public override ExecutionResult Run(string input, IChannel channel)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                var all = input.Trim().Split('\n', ';');
                foreach (var item in all)
                {
                    var kv = item.Split(new char[] { '=' }, 2);
                    var key = kv[0];
                    var value = kv.Length == 2 ? kv[1] : null;
                    this.Settings.Set(key, value);
                }
            }

            return this.Settings.All().ToArray().ToExecutionResult();
        }
    }
}
