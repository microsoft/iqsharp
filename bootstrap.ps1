# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.


# If the compiler constants include TELEMETRY, explicitly add the Aria telemetry package to the iqsharp tool:
if (($Env:ASSEMBLY_CONSTANTS -ne $null) -and ($Env:ASSEMBLY_CONSTANTS.Contains("TELEMETRY"))) {

    $project =  (Join-Path $PSScriptRoot 'src\Tool\Tool.csproj')
    $pkg =  "Microsoft.Applications.Events.Server.Core2"
    Write-Host "##[info]Adding $pkg to $project"
    dotnet add  $project `
        package $pkg `
        --no-restore `
        --version "$Env:BUILD_ARIA_VERSION"
}


