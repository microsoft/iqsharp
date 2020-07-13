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
        /property:InformationalVersion=$Env:SEMVER_VERSION `
        /property:Version=$Env:ASSEMBLY_VERSION

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test $project"
        $script:all_ok = $False
    }
}

function Test-Python {
    Param([string] $packageFolder, [string] $testFolder)

    Write-Host "##[info]Installing Python package from $packageFolder"
    Push-Location (Join-Path $PSScriptRoot $packageFolder)
        pip install .
    Pop-Location

    Write-Host "##[info]Installing IQ# kernel"
    Push-Location (Join-Path $PSScriptRoot '../src/Tool')
        dotnet run -- install --user
    Pop-Location

    Write-Host "##[info]Testing Python inside $testFolder"    
    Push-Location (Join-Path $PSScriptRoot $testFolder)
        python --version
        pytest -v --log-level=Debug
    Pop-Location

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test Python inside $testFolder"
        $script:all_ok = $False
    }
}

function Test-JavaScript {
    Param([string] $packageFolder, [string] $options)

    Write-Host "##[info]Installing JS packages from $packageFolder"
    Push-Location (Join-Path $PSScriptRoot $packageFolder)
        npm install
    Pop-Location

    Write-Host "##[info]Testing JS inside $packageFolder"    
    Push-Location (Join-Path $PSScriptRoot $packageFolder)
        if (!$options) {
            npm test
        } else {
            npm test -- $options
        }
    Pop-Location

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test JS inside $packageFolder"
        $script:all_ok = $False
    }
}

Test-One '../iqsharp.sln'

Test-Python '../src/Python' '../src/Python/qsharp/tests'

Test-JavaScript '../src/Kernel'

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}
