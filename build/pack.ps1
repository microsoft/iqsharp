# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

.\set-env.ps1

function Pack-One() {
    Param($project)
    dotnet pack $project `
        --no-build `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        -o $Env:NUGET_OUTDIR `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:PackageVersion=$Env:NUGET_VERSION

    if ($LastExitCode -ne 0) { throw "Cannot pack $project." }
}

Write-Host "##[info]Pack IQ# library:"
Pack-One '../src/Core/Core.csproj' 

Write-Host "##[info]Pack IQ# tool:"
Pack-One '../src/Tool/Tool.csproj'
