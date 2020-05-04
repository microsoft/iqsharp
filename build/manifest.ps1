#!/usr/bin/env pwsh
#Requires -PSEdition Core

& "$PSScriptRoot/set-env.ps1"

@{
    Packages = @(
        "Microsoft.Quantum.IQSharp.Core",
        "Microsoft.Quantum.IQSharp"
    );
    Assemblies = @(
        ".\src\tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.dll",
        ".\src\tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Core.dll",
        ".\src\tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Jupyter.dll",
        ".\src\tool\bin\$Env:BUILD_CONFIGURATION\netcoreapp3.1\Microsoft.Quantum.IQSharp.Web.dll"
    ) | ForEach-Object { Get-Item (Join-Path $PSScriptRoot ".." $_) };
} | Write-Output;