# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$failed = $false;

$Env:IQSHARP_PACKAGE_SOURCE = "$Env:NUGET_OUTDIR"

Push-Location (Resolve-Path $PSScriptRoot)
    pytest --junitxml=junit/integration-test-results.xml tests.py
    # Also test all the unit tests that came with the Python package itself.
    pytest --junitxml=junit/unit-test-results.xml (Resolve-Path (Join-Path (python -c "import qsharp; print(qsharp.__file__)") ".."))

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
