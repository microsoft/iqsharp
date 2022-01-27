// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp
{
    internal static class PlatformUtils
    {
        /// <returns>
        ///     The total amount of memory, if known, or <c>null</c> if
        ///     not known.
        /// </returns>
        public static Task<ulong?> GetTotalMemory(ILogger? logger = null)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return GetTotalMemoryViaWmi(logger);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return GetTotalMemoryViaFree(logger);
                }
                else
                {
                    logger?.LogWarning("Platform not recognized; not reporting total memory.");
                    return Task.FromResult<ulong?>(null);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception when getting total memory.");
                return Task.FromResult<ulong?>(null);
            }
        }

        #if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        #endif
        private static Task<ulong?> GetTotalMemoryViaWmi(ILogger? logger)
        {
            var query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            var searcher = new ManagementObjectSearcher(query);
            var managementObject = searcher.Get().Cast<ManagementObject>().Single();
            return Task.FromResult((ulong?)managementObject["TotalVirtualMemorySize"] * 1024);
        }

        #if NET5_0_OR_GREATER
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("osx")]
        #endif
        private static async Task<ulong?> GetTotalMemoryViaFree(ILogger? logger)
        {
            var processStartInfo = new ProcessStartInfo("free -m")
            {
                FileName = "/bin/bash",
                Arguments = "-c \"free --total --bytes\"",
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(processStartInfo);
            await foreach (var line in proc.StandardOutput.ReadAllLinesAsync())
            {
                if (line.StartsWith("Total:"))
                {
                    var parts = line.Split(" ");
                    return parts.Length >= 2
                           ? ulong.TryParse(parts[1], out var nBytes)
                             ? nBytes
                             : (ulong?)null
                           : (ulong?)null;
                }
            }
            return null;
        }
    }
}