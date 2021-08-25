# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [hashtable]
    $Properties = @{}
)

# Get PSake.
$psakeVersion = "4.9.0";
$psakePath = Join-Path $PSScriptRoot "vendor";
Save-Module -Name PSake -Path $psakePath -Force -RequiredVersion $psakeVersion;
Import-Module (Join-Path $psakePath "psake" $psakeVersion "psake.psm1");

Invoke-PSake -buildFile (Join-Path $PSScriptRoot "build" "psakefile.ps1") -taskList Bootstrap -properties $Properties;
