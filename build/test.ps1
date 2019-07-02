# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"

Write-Host "Testing IQ#:"
dotnet test ../iqsharp.sln `
    -c $Env:BUILD_CONFIGURATION `
    -v $Env:BUILD_VERBOSITY `
    --logger trx `
    /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
    /property:Version=$Env:ASSEMBLY_VERSION `
