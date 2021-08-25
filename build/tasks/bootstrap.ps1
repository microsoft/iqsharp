Properties {
    $EnableTelemetry = ($Env:ASSEMBLY_CONSTANTS -ne $null) -and ($Env:ASSEMBLY_CONSTANTS.Contains("TELEMETRY"));
    $AriaVersion = $Env:BUILD_ARIA_VERSION;
}

Task NpmInstall {
    # Fetch TypeScript definitions
    Push-Location (Join-Path $RepoRoot src/Kernel)
        npm install
    Pop-Location
}

Task InjectTelemetry -Precondition { $EnableTelemetry } -RequiredVariables AriaVersion {
    # If the compiler constants include TELEMETRY, explicitly add the Aria telemetry package to the iqsharp tool:
    $project =  (Join-Path $RepoRoot 'src\Tool\Tool.csproj')
    $pkg =  "Microsoft.Applications.Events.Server.Core2"
    Write-Host "##[info]Adding $pkg to $project"
    dotnet add  $project `
        package $pkg `
        --no-restore `
        --version "$AriaVersion"
}

<# TODO: Find a way to abstract (Join-Path $RepoRoot "devenv") into a property or parameter. #>

Task CreateDevEnvironment `
    <# Only create the dev environment if it doesn't already exist. #> `
    -Precondition { -not (Test-Path (Join-Path $RepoRoot "devenv")) } `
{
    Assert `
        -ConditionToCheck ((Get-Command conda -ErrorAction SilentlyContinue).Count -ne 0) `
        -FailureMessage "Creating the dev environment for this project requires conda to be installed and added to your shell."
    conda create --yes -p (Join-Path $RepoRoot "devenv") -c conda-forge conda-build antlr
}

Task RegenerateAntlr -Depends CreateDevEnvironment {
    Enter-CondaEnvironment (Join-Path $RepoRoot "devenv")
    Push-Location (Join-Path $RepoRoot "src" "CellParser" "grammar")
        Get-ChildItem *.g4 `
            | ForEach-Object {
                antlr4 -Dlanguage=CSharp $_
            }
    Pop-Location
    Exit-CondaEnvironment
}
