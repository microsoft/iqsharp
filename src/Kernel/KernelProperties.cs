// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    /// These are the list of properties for the Q# Jupyter Kernel.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     The properties for this kernel (e.g. versions, language name,
        ///     etc.).
        /// </summary>
        public static readonly KernelProperties IQSharpKernelProperties = new KernelProperties
        {
            FriendlyName = "Q#",
            KernelName = "iqsharp",
            KernelVersion =
                typeof(IQSharpEngine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(IQSharpEngine).Assembly.GetName().Version.ToString(),
            DisplayName = "Q#",

            LanguageName = "qsharp",
            LanguageVersion = "0.24",
            LanguageMimeType = "text/x-qsharp",
            LanguageFileExtension = ".qs",

            Description = "A kernel for the Q# language."
        };
    }
}
