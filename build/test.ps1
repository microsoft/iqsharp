# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

Write-Host "Testing IQ#:"

function Test-One {
    Param($project)

    Write-Host "##[info]Testing $project"
    dotnet test $project `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        --logger trx `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
}

Test-One '../iqsharp.sln'

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}

