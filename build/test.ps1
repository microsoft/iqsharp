# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

Write-Host "Testing IQ#:"

function Test-One {
    Param([string] $project, [string[]] $testClassFilters = @())

    Write-Host "##[info]Testing $project"
    if ($testClassFilters) {
        $filterArgs = $testClassFilters | foreach { "ClassName~$_" }
        $filterArgs += "(" + (($testClassFilters | foreach { "(ClassName!~$_)" }) -join " & ") + ")"
    }
    else {
        $filterArgs = @("FullyQualifiedName!=_fake_")
    }
    $filterArgs | foreach {
        dotnet test $project `
            -c $Env:BUILD_CONFIGURATION `
            -v $Env:BUILD_VERBOSITY `
            --no-build `
            --logger trx `
            --filter $_ `
            /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
            /property:InformationalVersion=$Env:SEMVER_VERSION `
            /property:Version=$Env:ASSEMBLY_VERSION

        if ($LastExitCode -ne 0) {
            Write-Host "##vso[task.logissue type=error;]Failed to test $project with filter $_"
            $script:all_ok = $False
        }
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
        dotnet run -c $Env:BUILD_CONFIGURATION --no-build -- install --user
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

Test-One '../iqsharp.sln' @("AzureClient", "IQSharpEngine", "Workspace")

Test-Python '../src/Python' '../src/Python/qsharp/tests'

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}
