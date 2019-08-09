# The user may not have run .NET Core SDK before, so we disable first-time
# experience to avoid capturing the NuGet cache.
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
export NUGET_XMLDOC_MODE=skip

dotnet tool install PowerShell --tool-path $PREFIX/bin --version $PKG_VERSION
