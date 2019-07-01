# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

.\set-env.ps1

Write-Host "##[info]Pack IQ# library:"
dotnet pack ../src/Core/Core.csproj `
    --no-build `
    -c $Env:BUILD_CONFIGURATION `
    -v $Env:BUILD_VERBOSITY `
    -o $Env:NUGET_OUTDIR `
    /property:Version=$Env:ASSEMBLY_VERSION `
    /property:PackageVersion=$Env:NUGET_VERSION

Write-Host "##[info]Pack IQ# tool:"
dotnet pack ../src/Tool/Tool.csproj `
    --no-build `
    -c $Env:BUILD_CONFIGURATION `
    -v $Env:BUILD_VERBOSITY `
    -o $Env:NUGET_OUTDIR `
    /property:Version=$Env:ASSEMBLY_VERSION `
    /property:PackageVersion=$Env:NUGET_VERSION
