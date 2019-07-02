# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

function Pack-Nuget() {
    param(
        [string]$project
    );

    dotnet pack (Join-Path $PSScriptRoot $project) `
        --no-build `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        -o $Env:NUGET_OUTDIR `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:PackageVersion=$Env:NUGET_VERSION

    return ($LastExitCode -eq 0)
}

function Pack-Wheel() {
    param(
        [string] $Path
    );

    $result = 0

    Push-Location (Join-Path $PSScriptRoot $Path)
        python setup.py bdist_wheel
        $result = $LastExitCode
        Copy-Item "dist/*-${Env:PYTHON_VERSION}-*.whl" $Env:NUGET_OUTDIR
    Pop-Location

    return ($result -eq 0)
}

function Pack-Image() {
    param(
        [string] $RepoName,
        [string] $Dockerfile
    );

    docker build `
        <# We treat $NUGET_OUTDIR as the build context, as we will need to ADD
           nuget packages into the image. #> `
        $Env:NUGET_OUTDIR `
        <# This means that the Dockerfile lives outside the build context. #> `
        -f (Join-Path $PSScriptRoot $Dockerfile) `
        <# Next, we tell Docker what version of IQ# to install. #> `
        --build-arg IQSHARP_VERSION=$Env:NUGET_VERSION `
        <# Finally, we tag the image with the current build number. #> `
        -t "${Env:DOCKER_PREFIX}${RepoName}:${Env:BUILD_BUILDNUMBER}"

    return ($LastExitCode -eq 0)
}

Write-Host "##[info]Packing IQ# library..."
$all_ok = (Pack-Nuget '../src/Core/Core.csproj') -and $all_ok

Write-Host "##[info]Packing IQ# tool..."
$all_ok = (Pack-Nuget '../src/Tool/Tool.csproj') -and $all_ok

Write-Host "##[info]Packing Python wheel..."
python --version
$all_ok = (Pack-Wheel '../src/Python/') -and $all_ok

Write-Host "##[info]Packing Docker image..."
$all_ok = (Pack-Image -RepoName "iqsharp-base" -Dockerfile '../images/iqsharp-base/Dockerfile') -and $all_ok

if (-not $all_ok) {
    throw "At least one package failed to build. Check the logs."
}
