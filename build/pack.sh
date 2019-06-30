#!/bin/bash 
set -x
set -e

. ./set-env.sh

# Pack IQ# library:
dotnet pack ../src/Core/Core.csproj \
    --no-build \
    -c $BUILD_CONFIGURATION \
    -v $BUILD_VERBOSITY \
    -o $NUGET_OUTDIR \
    /property:Version=$ASSEMBLY_VERSION \
    /property:PackageVersion=$NUGET_VERSION \

# Pack IQ# tool:
dotnet pack ../src/Tool/Tool.csproj \
    --no-build \
    -c $BUILD_CONFIGURATION \
    -v $BUILD_VERBOSITY \
    -o $NUGET_OUTDIR \
    /property:Version=$ASSEMBLY_VERSION \
    /property:PackageVersion=$NUGET_VERSION \
