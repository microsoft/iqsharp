# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$script:AllOk = $True

function Test-CondaPackage {
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline=$true)]
        [string]
        $Path
    );

    begin {
        if (-not (Get-Command conda-build -ErrorAction SilentlyContinue)) {
            Write-Host "##vso[task.logissue type=warning;] conda-build not installed. " + `
                       "Will skip testing conda packages.";
            return;
        }
        $OldPreference = $ErrorActionPreference;
        $ErrorActionPreference = "Continue";
    }

    process {
        Write-Host "##[info]Testing conda package $Path..."
        conda-build (Resolve-Path $Path) --test 2>&1 | ForEach-Object { "$_" };
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##vso[task.logissue type=error;]conda-build --test failed for $Path.";
            $script:AllOk = $false;
        }
    }

    end {
        $ErrorActionPreference = $OldPreference;
    }

}

Get-ChildItem $Env:CONDA_OUTDIR -Filter "*.tar.bz2" -Recurse | Select-Object -ExpandProperty FullName | Test-CondaPackage


if (-not $script:AllOk) {
    throw "At least one package failed to build. Check the logs."
}
