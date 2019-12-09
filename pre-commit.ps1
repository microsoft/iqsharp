##
# This script is executed before local changes are committed by the build
# to make sure things that should not be in source control are undo
##

# If the compiler constants include TELEMETRY, explicitly add the Aria telemetry package to the iqsharp tool:
if (($Env:ASSEMBLY_CONSTANTS -ne $null) -and ($Env:ASSEMBLY_CONSTANTS.Contains("TELEMETRY"))) {

  $project =  (Join-Path $PSScriptRoot 'src\Tool\Tool.csproj')
  $pkg =  "Microsoft.Applications.Events.Server.Core2"
  Write-Host "##[info]Remove $pkg from $project"
  dotnet remove  $project `
      package $pkg 


}