# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param (
    [Parameter()]
    [switch]
    $SkipInstall=$False
)

# For debug, print all relevant environment variables:
Get-ChildItem env:AZURE*, env:*VERSION, env:*OUTDIR | Format-Table | Out-String | Write-Host

if (-not $SkipInstall) {
    .\Install-Artifacts.ps1
}

# Install and run Pester
Import-Module Pester

$config = [PesterConfiguration]::Default
$config.Run.Exit = $true
$config.TestResult.Enabled = $true
$config.TestResult.OutputPath = "TestResults.xml"
$config.TestResult.OutputFormat = "JUnitXml"
$config.Output.Verbosity = "Detailed"

if (Test-Path Env:AZURE_QUANTUM_CAPABILITIES) {
    $config.Filter.Tag = $Env:AZURE_QUANTUM_CAPABILITIES -Split ";" | ForEach-Object { $_.trim() }
} else  {
    "Missing AZURE_QUANTUM_CAPABILITIES env variable. Will run all tests." | Write-Warning
}

Invoke-Pester -Configuration $config -Verbose