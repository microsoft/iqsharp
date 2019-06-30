#!/bin/bash 
set -e

: ${BUILD_BUILDNUMBER:="0.0.0.1"}
: ${BUILD_CONFIGURATION:="Debug"}
: ${BUILD_VERBOSITY:="m"}
: ${ASSEMBLY_VERSION:="$BUILD_BUILDNUMBER"}
: ${NUGET_VERSION:="$ASSEMBLY_VERSION-alpha"}
: ${NUGET_OUTDIR:="$(cd ..;pwd)/build.nugets"}

