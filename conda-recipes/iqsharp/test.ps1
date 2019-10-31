# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$failed = $false;

$Env:IQSHARP_PACKAGE_SOURCE = "$Env:NUGET_OUTDIR"

# Check that iqsharp is installed as a Jupyter kernel.
$kernels = jupyter kernelspec list --json | ConvertFrom-Json;
if ($null -eq $kernels.kernelspecs.iqsharp) {
    $failed = $true;
    Write-Error "##vso[task.logissue type=error;]Failed: IQ# not found in list of kernelspecs, see kernelspec list below."
    jupyter kernelspec list
}


Push-Location $PSScriptRoot
    python test.py
    if  ($LastExitCode -ne 0) {
        $failed = $true;
        Write-Host "##vso[task.logissue type=error;]Failed: IQ# kernel unittests failed"
    }
Pop-Location

# If any tests failed, raise an error code.
if ($failed) {
    exit -1;
} else {
    Write-Host "## ALL TESTS PASSED ##";
}
