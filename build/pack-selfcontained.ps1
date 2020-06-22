# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot "set-env.ps1")
$all_ok = $True

function Pack-Exe() {
    param(
        [string] $Project,
        [string] $Runtime,
        [string] $Configuration = $Env:BUILD_CONFIGURATION
    );

    $OutputPath = Join-Path $Env:SELFCONTAINED_OUTDIR $Runtime;

    Write-Host "##[info]Publishing self-contained $Runtime executable to $OutputPath"

    # Suppress generating pdb files.
    # See https://github.com/dotnet/cli/issues/2246#issuecomment-320633639.
    dotnet publish $Project `
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

& (Join-Path $PSScriptRoot ".." "bootstrap.ps1")

Write-Host "##[info]Packing IQ# as self-contained executables..."
Push-Location (Join-Path $PSScriptRoot ../src/Tool)
    Pack-Exe "./Tool.csproj" -Runtime win10-x64
    Pack-Exe "./Tool.csproj" -Runtime osx-x64
    Pack-Exe "./Tool.csproj" -Runtime linux-x64
Pop-Location

Write-Host "##[info]Verifying manifest..."
& (Join-Path $PSScriptRoot "manifest-selfcontained.ps1")
