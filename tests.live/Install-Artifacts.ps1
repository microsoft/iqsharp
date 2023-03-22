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
        # bootstrap takes care of installing all test requirements: 
        "Running bootstrap" | Write-Verbose
        .\bootstrap.ps1
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
    dotnet tool install --global Microsoft.Quantum.IQSharp --version 0.27.261851-beta --add-source $Env:NUGET_OUTDIR
    if ($LASTEXITCODE -ne 0) { throw "Error installing Microsoft.Quantum.IQSharp" }
    dotnet iqsharp install --user
    if ($LASTEXITCODE -ne 0) { throw "Error installing iqsharp kernel" }

    # Make sure the NUGET_OUTDIR is listed as a nuget source, otherwise
    # IQ# will fail to load when packages are loaded.
    $SourceName = "build"
    dotnet nuget add source $Env:NUGET_OUTDIR  --name $SourceName
    if ($LASTEXITCODE -ne 0) { 
        "Nuget source $SourceName already exist, will be updated to point to $($Env:NUGET_OUTDIR)" | Write-Warning
        dotnet nuget update source  $SourceName --source $Env:NUGET_OUTDIR
        if ($LASTEXITCODE -ne 0) { throw "Failed to update nuget config" }
    }

    # Install the qsharp-core wheel
    "Installing qsharp-core from $Env:PYTHON_OUTDIR" | Write-Verbose
    pip install qsharp-core==$Env:PYTHON_VERSION --find-links $Env:PYTHON_OUTDIR
    if ($LASTEXITCODE -ne 0) { throw "Error installing qsharp-core wheel" }
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
