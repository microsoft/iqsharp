#!/usr/bin/env pwsh

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
$BlobsDirectory = Join-Path $ArtifactRoot (Join-Path "blobs" $RuntimeID)

# Find where in the temporary environment we should install IQ# into.
if ($IsWindows) {
    $TargetDirectory = $Env:LIBRARY_BIN;
} else {
    $TargetDirectory = (Join-Path $Env:PREFIX "bin");
}

# If the target directory doesn't exist, create it before proceeding.
New-Item -Force -ItemType Directory -ErrorAction SilentlyContinue $TargetDirectory;

Write-Host "## Artifact manifest ($ArtifactRoot): ##"
Get-ChildItem -Recurse $ArtifactRoot | %{ Write-Host $_.FullName }

Write-Host "## Copying IQ# from '$BlobsDirectory' into '$TargetDirectory...' ##"
Copy-Item (Join-Path $BlobsDirectory "*") $TargetDirectory -Verbose;


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
