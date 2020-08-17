# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

function Pack-Nuget() {
    param(
        [string]$project
    );

    Write-Host "##[info]Packing $project..."

    dotnet pack (Join-Path $PSScriptRoot $project) `
        --no-build `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        -o $Env:NUGET_OUTDIR `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:InformationalVersion=$Env:SEMVER_VERSION `
        /property:PackageVersion=$Env:NUGET_VERSION

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to build $project."
        $script:all_ok = $False
    }
}

function Pack-Wheel() {
    param(
        [string] $Path
    );

    $result = 0

    Push-Location (Join-Path $PSScriptRoot $Path)
        python setup.py bdist_wheel sdist --formats=gztar

        if  ($LastExitCode -ne 0) {
            Write-Host "##vso[task.logissue type=error;]Failed to build $Path."
            $script:all_ok = $False
        } else {
            Copy-Item "dist/*.whl" $Env:PYTHON_OUTDIR
            Copy-Item "dist/*.tar.gz" $Env:PYTHON_OUTDIR
        }
    Pop-Location

}

function Pack-Image() {
    param(
        [string] $RepoName,
        [string] $Dockerfile
    );

    Try {
        docker version
    } Catch {
        Write-Host "##vso[task.logissue type=warning;]docker not installed. Will skip creation of image for $Dockerfile"
        return
    }

    
    <# If we are building a non-release build, we need to inject the
       prerelease feed as well.
       Note that since this will appear as an argument to docker build, which
       then evaluates the build argument using Bash, we
       need \" to be in the value of $extraNugetSources so that the final XML
       contains just a ". Thus the correct escape sequence is \`".
    #>
    if ("$Env:BUILD_RELEASETYPE" -ne "release") {
        $extraNugetSources = "<add key=\`"prerelease\`" value=\`"https://pkgs.dev.azure.com/ms-quantum-public/9af4e09e-a436-4aca-9559-2094cfe8d80c/_packaging/alpha%40Local/nuget/v3/index.json\`" />";
    } else {
        $extraNugetSources = "";
    }

    docker build `
        <# We treat $DROP_DIR as the build context, as we will need to ADD
           nuget packages into the image. #> `
        $Env:DROPS_DIR `
        <# This means that the Dockerfile lives outside the build context. #> `
        -f (Join-Path $PSScriptRoot $Dockerfile) `
        <# Next, we specify any additional NuGet sources to be used. #> `
        --build-arg EXTRA_NUGET_SOURCES="$extraNugetSources" `
        <# Next, we specify any additional NuGet packages that should be imported. #> `
        --build-arg EXTRA_NUGET_PACKAGES="" `
        <# Next, we tell Docker what version of IQ# to install. #> `
        --build-arg IQSHARP_VERSION=$Env:NUGET_VERSION `
        <# Finally, we tag the image with the current build number. #> `
        -t "${Env:DOCKER_PREFIX}${RepoName}:${Env:BUILD_BUILDNUMBER}"

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to create docker image for $Dockerfile."
        $script:all_ok = $False
    }
}

Pack-Nuget '../src/Core/Core.csproj'
Pack-Nuget '../src/ExecutionPathTracer/ExecutionPathTracer.csproj'
Pack-Nuget '../src/Jupyter/Jupyter.csproj'
Pack-Nuget '../src/Tool/Tool.csproj'

if ($Env:ENABLE_PYTHON -eq "false") {
    Write-Host "##vso[task.logissue type=warning;]Skipping Creating Python packages. Env:ENABLE_PYTHON was set to 'false'."
} else {
    Write-Host "##[info]Packing Python wheel..."
    python --version
    Pack-Wheel '../src/Python/qsharp-core'
}

# Figure out if we can run Docker or not.

if ($Env:ENABLE_DOCKER -eq "false") {
    Write-Host "##vso[task.logissue type=warning;]Skipping Creating Docker Image. Env:ENABLE_DOCKER was set to 'false'."
    $runDocker = $false;
} elseif (("$Env:AGENT_OS" -ne "") -and ($Env:AGENT_OS.StartsWith("Win"))) {
    Write-Host "--> Cannot create docker image on Windows."
    $runDocker = $false;
} else {
    $runDocker = $true;
}

if ($runDocker) {
    Write-Host "##[info]Packing Docker image..."
    Pack-Image -RepoName "iqsharp-base" -Dockerfile '../images/iqsharp-base/Dockerfile'
}

if (-not $runDocker -or ($Env:ENABLE_PYTHON -eq "false")) {
    Write-Host "##vso[task.logissue type=warning;]Skipping IQ# magic command documentation, either ENABLE_DOCKER or ENABLE_PYTHON was false.";
} else {
    Write-Host "##[info]Packing IQ# reference docs..."
    & (Join-Path $PSScriptRoot "pack-docs.ps1");
}
