# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$failed = $false;

$Env:IQSHARP_PACKAGE_SOURCE = "$Env:NUGET_OUTDIR"

# Add the prerelease NuGet feed if this isn't a release build.
if ("$Env:BUILD_RELEASETYPE" -ne "release") {
    $NuGetDirectory = Resolve-Path ~
    Write-Host "## Writing prerelease NuGet config to $NuGetDirectory ##"
    "<?xml version=""1.0"" encoding=""utf-8""?>
     <configuration>
        <packageSources>
            <add key=""qdk-alpha"" value=""https://pkgs.dev.azure.com/ms-quantum-public/Microsoft Quantum (public)/_packaging/alpha/nuget/v3/index.json"" protocolVersion=""3"" />
        </packageSources>
     </configuration>" | Out-File -FilePath $NuGetDirectory/NuGet.Config -Encoding utf8
}

# Check that iqsharp is installed as a Jupyter kernel.
$kernels = jupyter kernelspec list --json | ConvertFrom-Json;
if ($null -eq $kernels.kernelspecs.iqsharp) {
    $failed = $true;
    Write-Error "##vso[task.logissue type=error;]Failed: IQ# not found in list of kernelspecs, see kernelspec list below."
    jupyter kernelspec list
}

# Run the kernel unit tests.
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
