# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

function Pack-Exe() {
    param(
        [string] $Project,
        [string] $Runtime,
        [string] $Configuration = $Env:BUILD_CONFIGURATION
    );

    $OutputPath = Join-Path $Env:SELFCONTAINED_OUTDIR $Runtime;

    # Suppress generating pdb files.
    # See https://github.com/dotnet/cli/issues/2246#issuecomment-320633639.
    dotnet publish `
    (Join-Path $PSScriptRoot $Project) `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -o $OutputPath `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:PackAsTool=false `
        /property:CopyOutputSymbolsToPublishDirectory=false

}

Write-Host "##[info]Packing IQ# as self-contained executables."
Pack-Exe "../src/Tool/Tool.csproj" -Runtime win10-x64
Pack-Exe "../src/Tool/Tool.csproj" -Runtime osx-x64
Pack-Exe "../src/Tool/Tool.csproj" -Runtime linux-x64
