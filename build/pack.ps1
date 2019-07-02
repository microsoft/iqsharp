# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

function Pack-Nuget() {
    Param($project)
    dotnet pack $project `
        --no-build `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        -o $Env:NUGET_OUTDIR `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:PackageVersion=$Env:NUGET_VERSION

    return ($LastExitCode -eq 0)
}

Write-Host "##[info]Packing IQ# library..."
$all_ok = (Pack-Nuget '../src/Core/Core.csproj') -and $all_ok

Write-Host "##[info]Packing IQ# tool..."
$all_ok = (Pack-Nuget '../src/Tool/Tool.csproj') -and $all_ok


if (-not $all_ok) {
    throw "At least one package failed to build. Check the logs."
}