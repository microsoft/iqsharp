#!/usr/bin/env pwsh
param(
    [string]
    $Configuration = "Release",

    [string]
    $DotNetPath = $null
);

# This script is only designed to work on PowerShell Core or PowerShell 7 and later.
if ($PSVersionTable.PSEdition -eq "Desktop") {
    Write-Error "##vso[task.logissue type=error;]conda recipe for iqsharp requires PowerShell Core or PowerShell 7.";
}

# If we weren't given a path, find dotnet now.
# By default, look for the executable provided by the dotnetcore-sdk package,
# since the activate.d script isn't run by conda-build on all platforms.
if (($null -eq $DotNetPath) -or $DotNetPath.Length -eq 0) {
    $DotNetPath = (Get-Command dotnet).Source;
}

if (-not (Get-Command $DotNetPath)) {
    Write-Error "Could not find .NET Core SDK. This should never happen."
    exit -1;
}

# Set defaults for environment variables set from the host.
if ($null -eq $Env:ASSEMBLY_CONSTANTS) { $Env:ASSEMBLY_CONSTANTS = ""; }
if ($null -eq $Env:ASSEMBLY_VERSION) { $Env:ASSEMBLY_VERSION = "0.0.0.1"; }

# The user may not have run .NET Core SDK before, so we disable first-time
# experience to avoid capturing the NuGet cache.
$Env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "true";
$Env:NUGET_XMLDOC_MODE = "skip";

if ($IsWindows) {
    # NOTE: Building this package is ★only★ supported for Windows 10.
    $RuntimeID = "win10-x$Env:ARCH";
} elseif ($IsLinux) {
    $RuntimeID = "linux-x$Env:ARCH";
} elseif ($IsMacOS) {
    $RuntimeID = "osx-x$Env:ARCH";
}

$TargetDirectory = (Join-Path $Env:PREFIX "opt" "iqsharp");
$RepoRoot = Resolve-Path iqsharp;

Write-Host "## Diagnostic Information ##"
@{
    "Path to .NET Core SDK" = $DotNetPath;
    "Script root" = $PSScriptRoot;
    "Prefix" = $Env:PREFIX;
    "Runtime ID" = $RuntimeID;
    "Target directory" = $TargetDirectory;
    "Path to Jupyter" = (Get-Command jupyter -ErrorAction SilentlyContinue).Source;
    "Repo root" = $RepoRoot;
    "Assembly constants" = $Env:ASSEMBLY_CONSTANTS;
    "Assembly version" = $Env:ASSEMBLY_VERSION;
} | Format-Table | Out-String | Write-Host

# We need to disable <PackAsTool>true</PackAsTool> when publishing
# due to https://github.com/dotnet/cli/issues/10607. This should
# be resolved with .NET Core SDK 3.0 and later.
Write-Host "## Patching IQ# for Standalone Deployment. ##"
Copy-Item `
    (Join-Path $PSScriptRoot "ToolStandalone.csproj") `
    (Join-Path $RepoRoot "src" "Tool" "Tool.csproj") `
    -Verbose;

Write-Host "## Publishing IQ#. ##"
# Use dotnet publish to make a self-contained deployment of the IQ# tool.
Push-Location (Join-Path $RepoRoot src/Tool)
    # Check whether we need to rebuild or not.
    & $DotNetPath publish `
        --self-contained `
        -c $Configuration `
        -r $RuntimeID `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION `
        -o $TargetDirectory
Pop-Location

# Copy any DLLs in the dll-overrides directory into the target directory, allowing them
# to override the published versions.
$OverridePath = (Join-Path $PSScriptRoot "dll-overrides");
if (Test-Path $OverridePath -ErrorAction SilentlyContinue) {
    Get-ChildItem $OverridePath -Filter *.dll `
        | ForEach-Object {
            Write-Host "Overriding DLL: $_";
            Copy-Item -Verbose $_ $TargetDirectory;
        }
}

Write-Host "## Installing IQ# into Jupyter. ##"
$BaseName = "Microsoft.Quantum.IQSharp";
if ($IsWindows) {
    $BaseName = "${BaseName}.exe";
}
Push-Location $TargetDirectory
    $PathToTool = Resolve-Path "./$BaseName";
    & $PathToTool install --path-to-tool $PathToTool --sys-prefix
Pop-Location

Write-Host "## Manifest of Installed Files ##"
$ManifestFile = New-TemporaryFile | Select-Object -ExpandProperty FullName;
Get-ChildItem -Recurse $TargetDirectory | Out-File $ManifestFile;
Write-Host "##vso[task.uploadfile]$ManifestFile"
