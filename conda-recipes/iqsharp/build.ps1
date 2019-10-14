#!/usr/bin/env pwsh

if ($PSVersionTable.PSEdition -eq "Desktop") {
    $IsWindows = $true;
}

if ($IsWindows) {
    # NOTE: Building this package is ★only★ supported for Windows 10.
    $RuntimeID = "win10-x$Env:ARCH";
} elseif ($IsLinux) {
    $RuntimeID = "linux-x$Env:ARCH";
} elseif ($IsMacOS) {
    $RuntimeID = "osx-x$Env:ARCH";
}

# Find the repo root relative to this script.
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot ".." "..");
$ArtifactRoot = Join-Path $RepoRoot "drops";

# Find where in the temporary environment we should install IQ# into.
$TargetDirectory = (Join-Path $Env:PREFIX "bin");

Write-Host "## Artifact manifest: ##"
Get-ChildItem -Recurse $ArtifactRoot | Write-Host;

Write-Host "## Copying IQ# into $TargetDirectory... ##"
Copy-Item (Join-Path $ArtifactRoot "blobs" $RuntimeID "*") $TargetDirectory -Verbose;

Write-Host "## Installing IQ# into Jupyter. ##"
$BaseName = "Microsoft.Quantum.IQSharp";
if ($IsWindows) {
    $BaseName = "${BaseName}.exe";
}
Push-Location $TargetDirectory
    $PathToTool = Resolve-Path "./$BaseName";
    & $PathToTool install --path-to-tool $PathToTool --sys-prefix
Pop-Location
