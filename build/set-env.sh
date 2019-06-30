#!/bin/bash 
set -e
set -x

: ${BUILD_BUILDNUMBER:="0.0.0.1"}
echo "__vso[task.setvariable variable=BUILD_BUILDNUMBER]$BUILD_BUILDNUMBER"
: ${BUILD_CONFIGURATION:="Debug"}
echo "__vso[task.setvariable variable=BUILD_CONFIGURATION]$BUILD_CONFIGURATION"
: ${BUILD_VERBOSITY:="m"}
echo "__vso[task.setvariable variable=BUILD_VERBOSITY]$BUILD_VERBOSITY"
: ${IQSHARP_HOSTING_ENV:="dev-machine"}
echo "__vso[task.setvariable variable=IQSHARP_HOSTING_ENV]$IQSHARP_HOSTING_ENV"
: ${ASSEMBLY_VERSION:="$BUILD_BUILDNUMBER"}
echo "__vso[task.setvariable variable=ASSEMBLY_VERSION]$ASSEMBLY_VERSION"
: ${NUGET_VERSION:="$ASSEMBLY_VERSION-alpha"}
echo "__vso[task.setvariable variable=NUGET_VERSION]$NUGET_VERSION"
: ${NUGET_OUTDIR:="$(cd ..;pwd)/build.nugets"}
echo "__vso[task.setvariable variable=NUGET_OUTDIR]$NUGET_OUTDIR"

