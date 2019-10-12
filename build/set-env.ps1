# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

Write-Host "Setting up build environment variables"

If ($Env:BUILD_BUILDNUMBER -eq $null) { $Env:BUILD_BUILDNUMBER = "0.0.1.0" }
If ($Env:BUILD_CONFIGURATION -eq $null) { $Env:BUILD_CONFIGURATION = "Debug"}
If ($Env:BUILD_VERBOSITY -eq $null) { $Env:BUILD_VERBOSITY = "m"}
If ($Env:ASSEMBLY_VERSION -eq $null) { $Env:ASSEMBLY_VERSION = "$Env:BUILD_BUILDNUMBER"}
If ($Env:NUGET_VERSION -eq $null) { $Env:NUGET_VERSION = "$Env:ASSEMBLY_VERSION-alpha"}
If ($Env:PYTHON_VERSION -eq $null) { $Env:PYTHON_VERSION = "${Env:ASSEMBLY_VERSION}a1" }
if ($Env:DOCKER_PREFIX -eq $null) { $Env:DOCKER_PREFIX = "" }

If ($Env:DROPS_DIR -eq $null) { $Env:DROPS_DIR =  [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\drops")) }

If ($Env:NUGET_OUTDIR -eq $null) { $Env:NUGET_OUTDIR =  (Join-Path $Env:DROPS_DIR "nugets") }
If (-not (Test-Path -Path $Env:NUGET_OUTDIR)) { [IO.Directory]::CreateDirectory($Env:NUGET_OUTDIR) }

If ($Env:PYTHON_OUTDIR -eq $null) { $Env:PYTHON_OUTDIR =  (Join-Path $Env:DROPS_DIR "wheels") }
If (-not (Test-Path -Path $Env:PYTHON_OUTDIR)) { [IO.Directory]::CreateDirectory($Env:PYTHON_OUTDIR) }

If ($Env:BLOBS_OUTDIR -eq $null) { $Env:BLOBS_OUTDIR =  (Join-Path $Env:DROPS_DIR "blobs") }
If (-not (Test-Path -Path $Env:BLOBS_OUTDIR)) { [IO.Directory]::CreateDirectory($Env:BLOBS_OUTDIR) }

If ($Env:CONDA_OUTDIR -eq $null) { $Env:CONDA_OUTDIR =  (Join-Path $Env:DROPS_DIR "conda") }
If (-not (Test-Path -Path $Env:CONDA_OUTDIR)) { [IO.Directory]::CreateDirectory($Env:CONDA_OUTDIR) }

If ($Env:DOCS_OUTDIR -eq $null) { $Env:DOCS_OUTDIR =  (Join-Path $Env:DROPS_DIR "docs") }
If (-not (Test-Path -Path $Env:DOCS_OUTDIR)) { [IO.Directory]::CreateDirectory($Env:DOCS_OUTDIR) }

