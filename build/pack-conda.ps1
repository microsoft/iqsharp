# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

if ("true" -eq $Env:SYSTEM_DEBUG) {
    Set-PSDebug -Trace 1;
}

$CondaPlatform = (conda info --json) `
    | ConvertFrom-Json `
    | Select-Object -ExpandProperty platform;

# Write out diagnostics about what version of PowerShell we're on.
$PSVersionTable | Format-Table | Out-String | Write-Host;

function Pack-CondaRecipe() {
    param(
        [string] $Path,
        [string[]] $PythonVersions = @("3.7", "3.8")
    );

    if (-not (Get-Command conda-build -ErrorAction SilentlyContinue)) {
        Write-Host "##vso[task.logissue type=warning;] conda-build not installed. " + `
                   "Will skip creation of conda package for $Path.";
        return;
    }

    # conda-build prints some warnings to stderr, which can lead to false positives.
    # We wrap in a try-finally to make sure we can condition on the built file being there, and not on
    # writing to stderr.
    try {
        $OldPreference = $ErrorActionPreference;
        $ErrorActionPreference = "Continue";
        $PythonVersions | `
            ForEach-Object {
                Write-Host "##[info]Running: conda build $(Resolve-Path $Path) --python=$_"
                # See https://stackoverflow.com/a/20950421/267841 for why this works to force conda
                # to output all log messages to stdout instead of stderr.
                conda build (Resolve-Path $Path) --no-test --python=$_ 2>&1 | ForEach-Object { "$_" };
            }
    } catch {
        Write-Host "##vso[task.logissue type=warning;]conda build error: $_";
    } finally {
        $ErrorActionPreference = $OldPreference;
        $TargetDir = (Join-Path $Env:CONDA_OUTDIR $CondaPlatform);
        New-Item -ItemType Directory -Path $TargetDir -Force -ErrorAction SilentlyContinue;
        $PythonVersions | `
            ForEach-Object {
                $PackagePath = (conda build (Resolve-Path $Path) --python=$_ --output);
                Write-Host "##[debug]Expecting to find conda package at $PackagePath.";
                if (Test-Path $PackagePath) {
                    Copy-Item `
                        $PackagePath `
                        $TargetDir `
                        -ErrorAction Continue `
                        -Verbose;
                    Write-Host "##[info]Copied $PackagePath to $TargetDir.";
                } else {
                    Write-Host "##vso[task.logissue type=error;]Failed to create conda package for $Path (Python $_)."
                    $script:all_ok = $False
                }
            }
    }
}

Write-Host "##[info]Packing conda recipes..."

Pack-CondaRecipe -Path (Join-Path $PSScriptRoot "../conda-recipes/iqsharp")
Pack-CondaRecipe -Path (Join-Path $PSScriptRoot "../conda-recipes/qsharp")

if (-not $all_ok) {
    throw "At least one package failed to build. Check the logs."
}
