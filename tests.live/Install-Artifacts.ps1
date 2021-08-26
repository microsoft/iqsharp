# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
    .SYNOPSIS
        Installs prerequisites needed for IQ# Live tests.
        If the NUGET_OUTDIR environment variable is set, it will try to 
        install it from build artifacts (including Python wheels),
        otherwise it will install everything from source.
#>

<# 
    Install prerequisites.
#>
function Install-PreReqs() {
    "Intalling Pester" | Write-Verbose
    Install-Module -Name Pester -SkipPublisherCheck -Force -Scope CurrentUser

    Push-Location (Join-Path $PSScriptRoot ..)
        "Running bootstrap" | Write-Verbose
        .\bootstrap.ps1

        "Installing Python Pre-reqs" | Write-Verbose
        pip install `
            notebook nbconvert jupyter_client pytest
    Pop-Location
}

<# 
    Install IQ# From Source
#>
function Install-FromSource() {
    Push-Location (Join-Path $PSScriptRoot ..\src\Python\qsharp-core)
        "Installing qsharp-core from source" | Write-Verbose
        pip install -e .
    Pop-Location

    Push-Location (Join-Path $PSScriptRoot ..\src\Tool\)
        "Installing IQ# from source" | Write-Verbose
        dotnet run -- install --user
    Pop-Location
}

<# 
    Install IQ# From Build Artifacts
#>
function Install-FromBuild() {
    "Uninstalling IQ#" | Write-Verbose
    dotnet tool uninstall --global Microsoft.Quantum.IQSharp

    # Get the IQ# tool installed.
    "Installing IQ# from $Env:NUGET_OUTDIR using version $Env:NUGET_VERSION" | Write-Verbose
    dotnet tool install --global Microsoft.Quantum.IQSharp --version $Env:NUGET_VERSION --add-source $Env:NUGET_OUTDIR
    dotnet iqsharp install --user

    # Install the Q# wheels.
    Push-Location $Env:PYTHON_OUTDIR
        "Installing all wheels from $Env:PYTHON_OUTDIR" | Write-Verbose
        Get-ChildItem *.whl `
        | ForEach-Object {
            "Installing $_.Name" | Write-Verbose
            pip install --verbose --verbose $_.Name
        }
    Pop-Location
}


if (-not $Env:NUGET_OUTDIR) {
    "" | Write-Host
    "== Environment variable `$Env:NUGET_OUTDIR is not set. " | Write-Host
    "== We will install IQ# from source." | Write-Host
    "" | Write-Host
    Install-PreReqs
    Install-FromSource

    "" | Write-Host
    "== IQ# installed from source. ==" | Write-Host
    "" | Write-Host
} elseif (-not (Test-Path $Env:NUGET_OUTDIR)) {
    "" | Write-Warning
    "== The environment variable NUGET_OUTDIR is set, but pointing to an invalid location ($Env:NUGET_OUTDIR)" | Write-Warning
    "== To use build artifacts, download the artifacts locally and point the variable to this folder." | Write-Warning
    "" | Write-Warning
    Exit 1
} else {
    "== Preparing environment to use artifacts with version '$Env:NUGET_VERSION' " | Write-Host
    "== from '$Env:NUGET_OUTDIR' and '$Env:PYTHON_OUTDIR'" | Write-Host

    Install-PreReqs
    Install-FromBuild

    "" | Write-Host
    "== IQ# installed from build artifacts. ==" | Write-Host
    "" | Write-Host
}
