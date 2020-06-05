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

function Test-Python {
    Param([string] $directory)

    Write-Host "##[info]Testing Python inside $directory"
    
    Push-Location (Join-Path $PSScriptRoot $directory)
        python --version
        pytest --log-level=DEBUG
    Pop-Location

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test Python inside $directory"
        $script:all_ok = $False
    }
}

Test-One '../iqsharp.sln'

Test-Python '../src/Python/qsharp/tests'

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}

