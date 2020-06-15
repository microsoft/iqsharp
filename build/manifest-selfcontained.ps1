#!/usr/bin/env pwsh
#Requires -PSEdition Core

& "$PSScriptRoot/set-env.ps1"

@{
    Assemblies = @(
        "linux-x64/Microsoft.Quantum.IQSharp.dll",
        "linux-x64/Microsoft.Quantum.IQSharp.Core.dll",
        "linux-x64/Microsoft.Quantum.IQSharp.Jupyter.dll",
        "linux-x64/Microsoft.Quantum.IQSharp.Kernel.dll",
        "linux-x64/Microsoft.Quantum.IQSharp.Web.dll",
        "osx-x64/Microsoft.Quantum.IQSharp.dll",
        "osx-x64/Microsoft.Quantum.IQSharp.Core.dll",
        "osx-x64/Microsoft.Quantum.IQSharp.Jupyter.dll",
        "osx-x64/Microsoft.Quantum.IQSharp.Kernel.dll",
        "osx-x64/Microsoft.Quantum.IQSharp.Web.dll",
        "win10-x64/Microsoft.Quantum.IQSharp.dll",
        "win10-x64/Microsoft.Quantum.IQSharp.Core.dll",
        "win10-x64/Microsoft.Quantum.IQSharp.Jupyter.dll",
        "win10-x64/Microsoft.Quantum.IQSharp.Kernel.dll",
        "win10-x64/Microsoft.Quantum.IQSharp.Web.dll"
    ) | ForEach-Object { Get-Item (Join-Path $Env:SELFCONTAINED_OUTDIR $_) };
} | Write-Output;