# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.


# Fetch TypeScript definitions
Push-Location (Join-Path $PSScriptRoot src/Kernel)
    "##[info]Installing npm dependencies" | Write-Host
    npm install | Write-Host
    "==> npm install complete <==" | Write-Host
Pop-Location


# If the compiler constants include TELEMETRY, explicitly add the Aria telemetry package to the iqsharp tool:
if (($Env:ASSEMBLY_CONSTANTS -ne $null) -and ($Env:ASSEMBLY_CONSTANTS.Contains("TELEMETRY"))) {
    $project =  (Join-Path $PSScriptRoot 'src\Tool\Tool.csproj')
    $pkg =  "Microsoft.Applications.Events.Server.Core2"
    Write-Host "##[info]Adding $pkg to $project"
    dotnet add  $project `
        package $pkg `
        --no-restore `
        --version "$Env:BUILD_ARIA_VERSION"
}

# Install Python requirements for building/testing
if ($Env:ENABLE_PYTHON -ne "false") {
    $pythonVersion = python --version
    $requirements = Join-Path $PSScriptRoot 'src\Python\requirements.txt'
    "##[info]Installing requirements from '$requirements' using version: '{$pythonVersion}'" | Write-Host
    pip install -r  $requirements | Write-Host
    "==> pip install complete <==" | Write-Host
}