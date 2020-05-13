# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

& "$PSScriptRoot/set-env.ps1"

@{
    Packages = @(
        "Microsoft.Quantum.IQSharp.Core",
        "Microsoft.Quantum.IQSharp.Jupyter",
        "Microsoft.Quantum.IQSharp"
    );
    Assemblies = @(
        ".\src\Tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.dll",
        ".\src\Tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.AzureClient.dll",
        ".\src\Tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Core.dll",
        ".\src\Tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Jupyter.dll",
        ".\src\Tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Kernel.dll",
        ".\src\Tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Web.dll"
    ) | ForEach-Object { Get-Item (Join-Path $PSScriptRoot (Join-Path ".." $_)) };
} | Write-Output;