# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"

function Pack-One() {
    Param($project)
    dotnet pack (Join-Path $PSScriptRoot $project) `
        --no-build `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        -o $Env:NUGET_OUTDIR `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:PackageVersion=$Env:NUGET_VERSION

    if ($LastExitCode -ne 0) { throw "Cannot pack $project." }
}

function Pack-Wheel() {
    param(
        [string] $Path
    );

    Push-Location (Join-Path $PSScriptRoot $Path)
        python setup.py bdist_wheel
        Copy-Item "dist/*-${Env:PYTHON_VERSION}-*.whl" $Env:NUGET_OUTDIR
    Pop-Location
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
}

Write-Host "##[info]Pack IQ# library:"
Pack-One '../src/Core/Core.csproj' 

Write-Host "##[info]Pack IQ# tool:"
Pack-One '../src/Tool/Tool.csproj'

Write-Host "##[info]Pack Python wheel:"
python --version
Pack-Wheel '../src/Python/'

Write-Host "##[info]Packing Docker image:"
Pack-Image -RepoName "iqsharp-base" -Dockerfile '../images/iqsharp-base/Dockerfile'
