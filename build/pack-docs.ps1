# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

<#
    .SYNOPSIS
        Packs documentation for IQ# using a newly built Dockerfile and the
        build_docs.py script. See build/docs/README.md.
#>

param(

);

$ErrorActionPreference = 'Stop';
& "$PSScriptRoot/set-env.ps1";

# If we can, pack docs using the documentation build container.
# We use the trick at https://blog.ropnop.com/plundering-docker-images/#extracting-files
# to build a new image containing all the docs we care about, then `docker cp`
# them out.
$tempTag = New-Guid | Select-Object -ExpandProperty Guid;
# When building in release mode, we also want to document additional
# packages that contribute IQ# magic commands.
if ("$Env:BUILD_RELEASETYPE" -eq "release") {
    $extraPackages = "--package Microsoft.Quantum.Katas --package Microsoft.Quantum.Chemistry.Jupyter";
} else {
    $extraPackages = "";
}
# Note that we want to use a Dockerfile read from stdin so that we can more
# easily inject the right base image into the FROM line. In doing so,
# the build context should include the build_docs.py script that we need.
$dockerfile = @"
FROM ${Env:DOCKER_PREFIX}iqsharp-base:${Env:BUILD_BUILDNUMBER}

USER root
RUN pip install click ruamel.yaml
WORKDIR /workdir
RUN chown -R `${USER} /workdir

USER `${USER}
COPY build_docs.py /workdir
RUN python build_docs.py \
        /workdir/drops/docs/iqsharp-magic \
        microsoft.quantum.iqsharp.magic-ref \
        $extraPackages
"@;
$dockerfile | docker build -t $tempTag -f - (Join-Path $PSScriptRoot "docs");
$tempContainer = docker create $tempTag;
docker cp "${tempContainer}:/workdir/drops/docs/iqsharp-magic" (Join-Path $Env:DOCS_OUTDIR "iqsharp-magic")
