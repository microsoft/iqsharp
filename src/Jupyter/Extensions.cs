// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public static class Extensions
    {
        /// <summary>
        /// Creates a wrapper of an IChannel that adds new lines to every message
        /// sent to stdout and stderr
        /// </summary>
        public static ChannelWithNewLines WithNewLines(this IChannel original) =>
            (original is ChannelWithNewLines ch) ? ch : new ChannelWithNewLines(original);

        public static void AddIQSharpKernel(this IServiceCollection services)
        {
            services.AddSingleton<ISymbolResolver, Jupyter.SymbolResolver>();
            services.AddSingleton<IExecutionEngine, Jupyter.IQSharpEngine>();
            services.AddSingleton<IConfigurationSource, Jupyter.IQSharpEngine>(
                provider => provider.GetService<IExecutionEngine>() as IQSharpEngine
            );
        }

        internal static IConfigurationSource ApplyConfiguration<T>(
            this IConfigurationSource configurationSource,
            string keyName, Action<T> action
        )
        {
            if (configurationSource.Configuration.TryGetValue(keyName, out var value))
            {
                action(value.ToObject<T>());
            }
            return configurationSource;
        }
    }
}
