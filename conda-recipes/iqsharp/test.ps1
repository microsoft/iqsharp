$failed = $false;

# Check that iqsharp is installed as a Jupyter kernel.
$kernels = jupyter kernelspec list --json | ConvertFrom-Json;
if ($null -eq $kernels.kernelspecs.iqsharp) {
    $failed = $true;
    Write-Error "IQ# not found in list of kernelspecs, found $($kernels.kernelspecs.PSObject.Properties.Name)."
}

# If any tests failed, raise an error code.
if ($failed) {
    return -1;
}
