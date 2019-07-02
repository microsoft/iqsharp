# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"

Write-Host "Build IQ#:"
dotnet build (Join-Path $PSScriptRoot ../iqsharp.sln) `
    -c $Env:BUILD_CONFIGURATION `
    -v $Env:BUILD_VERBOSITY `
    /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
    /property:Version=$Env:ASSEMBLY_VERSION
