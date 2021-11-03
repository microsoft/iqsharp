// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Quantum.IQSharp
{
    public class NormalizedEnvironmentVariableConfigurationSource : IConfigurationSource
    {
        public IDictionary<string, string>? Aliases { get; set; }
        public string? Prefix { get; set; }
        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new NormalizedEnvironmentVariableConfigurationProvider(
                Prefix, Aliases
            );
    }

    public class NormalizedEnvironmentVariableConfigurationProvider : ConfigurationProvider
    {
        private readonly IImmutableDictionary<string, string> Aliases;
        private readonly string Prefix;

        public NormalizedEnvironmentVariableConfigurationProvider(
            string? prefix = null,
            IDictionary<string, string>? aliases = null
        )
        {
            Aliases = aliases?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase)
                      ?? ImmutableDictionary<string, string>.Empty;
            Prefix = prefix ?? "";
        }

        public override void Load() =>
            Data = System.Environment
                .GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Where(variable =>
                    ((string)variable.Key).StartsWith(Prefix)
                )
                .ToDictionary(
                    variable => {
                        var keyWithoutPrefix = ((string)variable.Key)
                            .Substring(Prefix.Length);
                        if (Aliases.TryGetValue(keyWithoutPrefix, out var newKey))
                        {
                            return newKey;
                        }
                        return keyWithoutPrefix;
                    },
                    variable => ((string)variable.Value!),
                    StringComparer.OrdinalIgnoreCase
                );
    }
}