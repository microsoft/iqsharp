#!/usr/bin/env pwsh
param(
    [string]
    $Configuration = "Release",

    [string]
    $DotNetPath = $null
);

# On Windows PowerShell, we don't have the nice $IsWindows / $IsLinux / $IsMacOS
# variables. Since Windows PowerShell is the only PowerShell variant that
# has "Desktop" as its PSEdition, we can check for that and create the
# $IsWindows variable if need be.
if ($PSVersionTable.PSEdition -eq "Desktop") {
    $IsWindows = $true;
}

# If we weren't given a path, find dotnet now.
# By default, look for the executable provided by the dotnetcore-sdk package,
# since the activate.d script isn't run by conda-build on all platforms.
if (($null -eq $DotNetPath) -or $DotNetPath.Length -eq 0) {
    $CondaDotNet = Join-Path (Join-Path (Join-Path $Env:PREFIX "opt") "dotnet") "dotnet";
    if (Get-Command $CondaDotNet -ErrorAction SilentlyContinue) {
        $DotNetPath = $CondaDotNet;
    } else {
        # Check for a globally installed fallback and warn.
        Write-Warning "Could not find dotnet as installed by the dotnetcore-sdk package. Falling back to global version.";
        $DotNetPath = (Get-Command dotnet).Source;
    }
}

if (-not (Get-Command $DotNetPath)) {
    Write-Error "Could not find .NET Core SDK. This should never happen."
    exit -1;
}

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

# On PowerShell 6 (Core) and later, Join-Path takes multiple child paths,
# but we don't use that for compatability with Windows PowerShell (5.1 and
# earlier).
$TargetDirectory = (Join-Path (Join-Path $Env:PREFIX "opt") "iqsharp");
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
} | Format-Table | Out-String | Write-Host

# We need to disable <PackAsTool>true</PackAsTool> when publishing
# due to https://github.com/dotnet/cli/issues/10607. This should
# be resolved with .NET Core SDK 3.0 and later.
Write-Host "## Patching IQ# for Standalone Deployment. ##"
Copy-Item (Join-Path $PSScriptRoot "ToolStandalone.csproj") (Join-Path (Join-Path (Join-Path $RepoRoot "src") "Tool") "Tool.csproj");

Write-Host "## Building IQ#. ##"
Push-Location (Join-Path $RepoRoot src/Tool)
    & $DotNetPath publish --self-contained -c $Configuration -r $RuntimeID -o $TargetDirectory
Pop-Location

Write-Host "## Installing IQ# into Jupyter. ##"
$BaseName = "Microsoft.Quantum.IQSharp";
if ($IsWindows) {
    $BaseName = "${BaseName}.exe";
}
Push-Location $TargetDirectory
    $PathToTool = Resolve-Path "./$BaseName";
    & $PathToTool install --path-to-tool $PathToTool --sys-prefix
Pop-Location
