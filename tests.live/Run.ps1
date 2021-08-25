# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param (
    [Parameter()]
    [switch]
    $SkipInstall=$False
)

if (-not $SkipInstall) {
    .\Install-Artifacts.ps1
}

# For debug, print all relevant environment variables:
Get-ChildItem env:AZURE*, env:*VERSION | Format-Table | Out-String | Write-Host

# Install and run Pester
Import-Module Pester

$config = [PesterConfiguration]::Default
$config.Run.Exit = $true
$config.TestResult.Enabled = $true
$config.TestResult.OutputPath = "TestResults.xml"
$config.TestResult.OutputFormat = "JUnitXml"
$config.Output.Verbosity = "Detailed"

Invoke-Pester -Configuration $config -Verbose