# Copyright (c) Microsoft Corporation.
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

$TargetDirectory = (Join-Path (Join-Path $Env:PREFIX "opt") "iqsharp");

# If the target directory doesn't exist, create it before proceeding.
New-Item -Force -ItemType Directory -ErrorAction SilentlyContinue $TargetDirectory;

Write-Host "## Artifact manifest ($ArtifactRoot): ##"
Get-ChildItem -Recurse $ArtifactRoot | ForEach-Object { Write-Host $_.FullName }

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
    $InstallCmd = "./$BaseName install --path-to-tool $(Resolve-Path $PathToTool) --sys-prefix --user-agent-extra `"(iqsharp:conda)`"";
    Write-Host "$ $InstallCmd";
    Invoke-Expression $InstallCmd;
Pop-Location

Write-Host "## Installing OpenMP support into library path. ##"
if ($IsLinux) {
    Copy-Item -Verbose `
        (Join-Path (Join-Path (Join-Path $TargetDirectory "runtimes") $RuntimeID) "libomp.so.*") `
        (Join-Path (Join-Path $Env:PREFIX "lib") "x86_64-linux-gnu")
} elseif ($IsMacOS) {
    Copy-Item -Verbose `
        (Join-Path (Join-Path (Join-Path $TargetDirectory "runtimes") $RuntimeID) "libomp.dylib") `
        (Join-Path (Join-Path $Env:PREFIX "lib") "x86_64-darwin-macho")    
}
