#!/bin/bash 
set -x
set -e

. ./set-env.sh

# Test IQ#:
dotnet test ../iqsharp.sln \
    -c $BUILD_CONFIGURATION \
    -v $BUILD_VERBOSITY \
    --logger trx \
    /property:DefineConstants=$ASSEMBLY_CONSTANTS \
    /property:Version=$ASSEMBLY_VERSION \
