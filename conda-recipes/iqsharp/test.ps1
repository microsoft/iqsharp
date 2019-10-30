# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$failed = $false;

# Check that iqsharp is installed as a Jupyter kernel.
$kernels = jupyter kernelspec list --json | ConvertFrom-Json;
if ($null -eq $kernels.kernelspecs.iqsharp) {
    $failed = $true;
    Write-Error "## TEST FAILED: IQ# not found in list of kernelspecs, see kernelspec list below."
    jupyter kernelspec list
}

# If any tests failed, raise an error code.
if ($failed) {
    exit -1;
} else {
    Write-Host "## ALL TESTS PASSED ##";
}
