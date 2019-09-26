# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# If the compiler constants include TELEMETRY, explicitly add the Aria telemetry package to the iqsharp tool:
if (($Env:ASSEMBLY_CONSTANTS -ne $null) -and ($Env:ASSEMBLY_CONSTANTS.Contains("TELEMETRY"))) {

    $projects = @('src\Tool\Tool.csproj', 'conda-packages\iqsharp\ToolStandalone.csproj');
    $pkg =  "Microsoft.Applications.Events.Server.Core2"
    Write-Host "##[info]Adding $pkg to $project"
    $projects | ForEach-Object {
        dotnet add `
            (Join-Path $PSScriptRoot $_) `
            package $pkg `
            --no-restore `
            --version "0.92.6"
    }
}
