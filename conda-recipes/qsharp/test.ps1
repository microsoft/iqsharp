# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$failed = $false;

$Env:IQSHARP_PACKAGE_SOURCE = "$Env:NUGET_OUTDIR"

Push-Location (Resolve-Path $PSScriptRoot)
    pytest --junitxml=junit/integration-test-results.xml tests.py
    # Also test all the unit tests that came with the Python package itself.
    # NB: We disable most tests for now, since many of the tests require Q# code that
    #     isn't copied into the built package.
    #     Instead, we enable only those tests we know are relocatable.
    $testPath = Join-Path $Env:SRC_DIR "src/Python/qsharp-core/qsharp/tests/test_iqsharp.py";
    $cmd = "pytest --junitxml=junit/unit-test-results.xml `"${testPath}::TestCaptureDiagnostics`"";
    Write-Host "##[debug]Running integration tests with command: $cmd"
    Invoke-Expression $cmd;

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
