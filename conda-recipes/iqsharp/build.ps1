# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

if ($PSVersionTable.PSEdition -eq "Desktop") {
    $IsWindows = $true;
}

if ($IsWindows) {
    $RuntimeID = "win10-x$Env:ARCH";
} elseif ($IsLinux) {
    $RuntimeID = "linux-x$Env:ARCH";
} elseif ($IsMacOS) {
    $RuntimeID = "osx-x$Env:ARCH";
}

# Find the repo root relative to this script.
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..");
$ArtifactRoot = Join-Path $RepoRoot "drops";
$SelfContainedDirectory = Join-Path $ArtifactRoot (Join-Path "selfcontained" $RuntimeID)
$NugetsDirectory = Join-Path $ArtifactRoot "nugets"
$NugetConfig = Resolve-Path (Join-Path $PSScriptRoot "NuGet.config");

$TargetDirectory = (Join-Path (Join-Path $Env:PREFIX "opt") "iqsharp");

# If the target directory doesn't exist, create it before proceeding.
New-Item -Force -ItemType Directory -ErrorAction SilentlyContinue $TargetDirectory;

Write-Host "## Artifact manifest ($ArtifactRoot): ##"
Get-ChildItem -Recurse $ArtifactRoot | %{ Write-Host $_.FullName }

Write-Host "## Copying IQ# from '$SelfContainedDirectory' into '$TargetDirectory...' ##"
Copy-Item (Join-Path $SelfContainedDirectory "*") $TargetDirectory -Verbose -Recurse -Force;

Write-Host "## Installing IQ# into Jupyter. ##"
$BaseName = "Microsoft.Quantum.IQSharp";
if ($IsWindows) {
    $BaseName = "${BaseName}.exe";
}
Push-Location $TargetDirectory
    $PathToTool = Resolve-Path "./$BaseName";
    Write-Host "Path to IQ# kernel: $PathToTool";

    # If we're not on Windows, we need to make sure that the program is marked
    # as executable.
    if (-not $IsWindows) {
        Write-Host "Setting IQ# kernel to be executable.";
        chmod +x $PathToTool;
    }

    # Report the item as copied to the target directory.
    Get-Item $PathToTool;

    # Build up an install command to execute.
    & "./$BaseName" --version;
    $InstallCmd = "./$BaseName install --path-to-tool $(Resolve-Path $PathToTool) --sys-prefix";
    Write-Host "$ $InstallCmd";
    Invoke-Expression $InstallCmd;
Pop-Location
