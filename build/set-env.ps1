# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

Write-Host "Setting up build environment variables"

If ($Env:BUILD_BUILDNUMBER -eq $null) { $Env:BUILD_BUILDNUMBER = "0.0.0.1" }
If ($Env:BUILD_CONFIGURATION -eq $null) { $Env:BUILD_CONFIGURATION = "Debug"}
If ($Env:BUILD_VERBOSITY -eq $null) { $Env:BUILD_VERBOSITY = "m"}
If ($Env:ASSEMBLY_VERSION -eq $null) { $Env:ASSEMBLY_VERSION = "$Env:BUILD_BUILDNUMBER"}
If ($Env:NUGET_VERSION -eq $null) { $Env:NUGET_VERSION = "$Env:ASSEMBLY_VERSION-alpha"}
If ($Env:PYTHON_VERSION -eq $null) { $Env:PYTHON_VERSION = "${Env:ASSEMBLY_VERSION}a1" }
if ($Env:DOCKER_PREFIX -eq $null) { $Env:DOCKER_PREFIX = "" }

If ($Env:NUGET_OUTDIR -eq $null) { $Env:NUGET_OUTDIR =  [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\drop\nugets")) }
If (-not (Test-Path -Path $Env:NUGET_OUTDIR)) { md -Force $Env:NUGET_OUTDIR }

If ($Env:PYTHON_OUTDIR -eq $null) { $Env:PYTHON_OUTDIR =  [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\drop\wheels")) }
If (-not (Test-Path -Path $Env:PYTHON_OUTDIR)) { md -Force $Env:PYTHON_OUTDIR }
