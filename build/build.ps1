# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

& "$PSScriptRoot/set-env.ps1"

$all_ok = $True

function Build-One {
    param(
        [string]$action,
        [string]$project
    );

    Write-Host "##[info]Building $project"
    dotnet $action (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:InformationalVersion=$Env:SEMVER_VERSION `
        /property:QsharpDocsOutDir=$Env:DOCS_OUTDIR

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to build $project"
        $script:all_ok = $False
    }
}

# Fetch TypeScript definitions
Push-Location (Join-Path $PSScriptRoot ../src/Kernel)
    npm install
Pop-Location

Build-One build '../iqsharp.sln'

Write-Host "##[info]Verifying manifest..."
& (Join-Path $PSScriptRoot "manifest.ps1")

if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}
