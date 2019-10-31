# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$failed = $false;

$Env:IQSHARP_PACKAGE_SOURCE = "$Env:NUGET_OUTDIR"

Push-Location (Resolve-Path $PSScriptRoot)
    pytest --junitxml=junit/test-results.xml tests.py

    # Check for success.
    if  ($LastExitCode -ne 0) {
        $failed = $true;
        Write-Host "##vso[task.logissue type=error;]Failed: Invoking qsharp from python."
    }
Pop-Location


# If any tests failed, raise an error code.
if ($failed) {
    exit -1;
} else {
    Write-Host "## ALL TESTS PASSED ##";
}
