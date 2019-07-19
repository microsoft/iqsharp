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

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test $project"
        $script:all_ok = $False
    }
}

Test-One '../iqsharp.sln'

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}

