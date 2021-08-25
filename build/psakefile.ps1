Properties {
    $RepoRoot = Join-Path $PSScriptRoot "..";
}

task ? -Description "Helper to display task info" {
    Write-Documentation
}

Task Bootstrap -Depends NpmInstall, InjectTelemetry, RegenerateAntlr

Include (Join-Path $PSScriptRoot "tasks" "bootstrap.ps1")
