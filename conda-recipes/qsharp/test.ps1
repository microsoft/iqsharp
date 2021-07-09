# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$failed = $false;

$Env:IQSHARP_PACKAGE_SOURCE = "$Env:NUGET_OUTDIR"
$Env:IQSHARP_LOG_LEVEL = "Debug"

Push-Location (Resolve-Path $PSScriptRoot)
    $report = Join-Path (Resolve-Path .) "conda-integration-test-results.md";
    pytest --md $report -v --log-level=Debug tests.py
    Write-Host "##vso[task.uploadsummary]$report"

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
