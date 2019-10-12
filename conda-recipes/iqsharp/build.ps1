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

$TargetDirectory = (Join-Path $Env:PREFIX "bin");
$ArtifactRoot = Resolve-Path drops;

Write-Host "## Copying IQ# into $TargetDirectory... ##"
Copy-Item (Join-Path $ArtifactRoot "blobs" $RuntimeID "*") $TargetDirectory;

Write-Host "## Installing IQ# into Jupyter. ##"
$BaseName = "Microsoft.Quantum.IQSharp";
if ($IsWindows) {
    $BaseName = "${BaseName}.exe";
}
Push-Location $TargetDirectory
    $PathToTool = Resolve-Path "./$BaseName";
    & $PathToTool install --path-to-tool $PathToTool --sys-prefix
Pop-Location
