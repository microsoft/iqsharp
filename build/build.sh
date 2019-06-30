#!/bin/bash 
set -x
set -e

. ./set-env.sh

# Build IQ#:
dotnet build ../iqsharp.sln \
    -c $BUILD_CONFIGURATION \
    -v $BUILD_VERBOSITY \
    /property:DefineConstants=$ASSEMBLY_CONSTANTS \
    /property:Version=$ASSEMBLY_VERSION \
