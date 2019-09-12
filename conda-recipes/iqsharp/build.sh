echo "Using $(which pwsh) to run build.ps1"
echo "Using .NET Core SDK at $(which dotnet)"
# We need to pass the path to dotnet manually,
# as the path environment variable is sometimes clobbered when
# running the build script.
pwsh iqsharp/conda-recipes/iqsharp/build.ps1 -DotNetPath "$(which dotnet)"
