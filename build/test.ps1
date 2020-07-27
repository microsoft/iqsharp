# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

Write-Host "Testing IQ#:"

function Test-One {
    <#
    .SYNOPSIS
        Runs dotnet test for the specified project, optionally
        split into separate runs using the specified filters.
    .DESCRIPTION
        This function always runs all tests contained in the specified project.
        
        If no testClassFilters are specified, then all tests are run as part of
        a single dotnet test invocation.

        If testClassFilters is not empty, then dotnet test will be run multiple times:
        - once per filter using `--filter ClassName~filter1`, `--filter ClassName~filter2`,
          etc., where `filter1`, etc. represents each of the items in testClassFilters.
        - once with `--filter (ClassName!~filter1 & ClassName!~filter2)`, where the
          filter string runs all remaining tests and excludes all of the tests that were
          previously run.
    #>
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
        Write-Host "##[info]Testing $project with filter $_"

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

if ($Env:ENABLE_PYTHON -eq "false") {
    Write-Host "##vso[task.logissue type=warning;]Skipping Testing Python packages. Env:ENABLE_PYTHON was set to 'false'."
} else {
    Test-Python '../src/Python/qsharp-core' '../src/Python/qsharp-core/qsharp/tests'
}

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}
