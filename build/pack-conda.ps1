# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True
$CondaPlatform = (conda info --json) `
    | ConvertFrom-Json `
    | Select-Object -ExpandProperty platform;

# Detect if we're running on Windows. This is trivial on PowerShell Core, but
# takes a bit more work on Windows PowerShell.
if ("Desktop" -eq $PSVersionTable.PSEdition) {
    $IsWindows = $true;
}

function Pack-CondaRecipe() {
    param(
        [string] $Path
    );

    $LogFile = New-TemporaryFile;
    if (-not (Get-Command conda-build -ErrorAction SilentlyContinue)) {
        Write-Host "##vso[task.logissue type=warning;] conda-build not installed. " + `
                   "Will skip creation of conda package for $Path.";
        return;
    }

    # conda-build prints some warnings to stderr, which can lead to false positives.
    # We wrap in a try-finally to make sure we can condition on the exit code and not on
    # writing to stderr.
    try {
        Write-Host "##[info]Running: conda build $(Resolve-Path $Path)"
        conda build (Resolve-Path $Path) *>&1 `
            | Tee-Object -FilePath $LogFile.FullName;
    } catch {
        Write-Host "##vso[task.logissue type=warning;]$_";
    } finally {
        Write-Host "##[vso.uploadfile]$($LogFile.FullPath)"
        Write-Host "[vso.uploadfile]$($LogFile.FullPath)"
        if ($LastExitCode -ne 0) {
            Write-Host "##vso[task.logissue type=error;]Failed to create conda package for $Path."
            $script:all_ok = $False
        } else {
            $TargetDir = (Join-Path $Env:CONDA_OUTDIR $CondaPlatform);
            New-Item -ItemType Directory -Path $TargetDir -Force -ErrorAction SilentlyContinue;
            Copy-Item `
                (conda build (Resolve-Path $Path) --output) `
                $TargetDir `
                -ErrorAction Continue `
                -Verbose;
        }
    }
}

Write-Host "##[info]Packing conda recipes..."
Pack-CondaRecipe -Path "../conda-recipes/dotnetcore-sdk"
if (-not $IsWindows) {
    Pack-CondaRecipe -Path "../conda-recipes/pwsh"
}
Pack-CondaRecipe -Path "../conda-recipes/iqsharp"
Pack-CondaRecipe -Path "../conda-recipes/qsharp"

if (-not $all_ok) {
    throw "At least one package failed to build. Check the logs."
}
