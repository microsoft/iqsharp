#!/usr/bin/env pwsh
param(
    [string]
    $Configuration = "Release"
);

if ($IsWindows) {
    # NOTE: Building this package is ★only★ supported for Windows 10.
    $RuntimeID = "win10-x$Env:ARCH";
} elseif ($IsLinux) {
    $RuntimeID = "linux-x$Env:ARCH";
} elseif ($IsMacOS) {
    $RuntimeID = "osx-x$Env:ARCH";
}

$TargetDirectory = (Join-Path $Env:PREFIX "iqsharp");
$RepoRoot = Resolve-Path iqsharp;

Write-Host "## Diagnostic Information ##"
@{
    "Prefix" = $Env:PREFIX;
    "Runtime ID" = $RuntimeID;
    "Target directory" = $TargetDirectory;
    "Path to Jupyter" = (Get-Command jupyter -ErrorAction SilentlyContinue);
    "Repo root" = $RepoRoot;
} | Format-Table | Write-Host

Write-Host "## Building IQ#. ##"
Push-Location (Join-Path $RepoRoot src/Tool)
    dotnet publish --self-contained -c $Configuration -r $RuntimeID -o $TargetDirectory
Pop-Location

Write-Host "## Installing IQ# into Jupyter. ##"
Push-Location $TargetDirectory
    $BaseName = "Microsoft.Quantum.IQSharp";
    if ($IsWindows) {
        $BaseName = "${BaseName}.exe";
    }
    $PathToTool = Resolve-Path "./$BaseName";
    & $PathToTool install --path-to-tool $PathToTool --sys-prefix
Pop-Location
