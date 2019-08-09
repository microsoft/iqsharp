set DOTNET_HOME=%PREFIX%\opt\dotnet

@REM We use powershell.exe and not pwsh.exe to invoke the downloaded script,
@REM as we do not know whether or not the build agent has PowerShell 6 or later
@REM installed.
powershell.exe -Command "./dotnet-install.ps1 -Version %PKG_VERSION% -NoPath -InstallDir %DOTNET_HOME%"

@REM We can save environment variables into the new package using the technique
@REM demonstrated at:
@REM     https://github.com/conda-forge/staged-recipes/pull/2002/files
@REM We do so here to ensure that the new install of .NET Core SDK is added
@REM to %PATH%, and that the %DOTNET_HOME% variable is set for use with global
@REM tools.

@REM Since the activate.d mechanism is shell-dependent, we will also need to
@REM provide activation and deactivation scripts for cmd.exe, PowerShell, and
@REM for POSIX-style shells.

@REM In the future, we should transition this mechanism over to use what is
@REM implemented to resolve the shell-dependence of the current activate.d
@REM system. See also:
@REM     https://github.com/conda/conda/issues/6820.

set ACT_D=%PREFIX%\etc\conda\activate.d
md %ACT_D%
set DEACT_D=%PREFIX%\etc\conda\deactivate.d
md %DEACT_D%

@REM -- cmd activation/deactivation --
echo set _CONDA_PKG_BACKUP_PATH=%%PATH%% >> %ACT_D%\dotnet_home.cmd
echo set _CONDA_PKG_BACKUP_DOTNET_HOME=%%DOTNET_HOME%% >> %ACT_D%\dotnet_home.cmd
echo set DOTNET_HOME=%DOTNET_HOME% >> %ACT_D%\dotnet_home.cmd
echo set PATH=%PATH%;%DOTNET_HOME% >> %ACT_D%\dotnet_home.cmd
echo set PATH=%%_CONDA_PKG_BACKUP_PATH%% >> %DEACT_D%\dotnet_home.cmd
echo set DOTNET_HOME=%%_CONDA_PKG_BACKUP_DOTNET_HOME%% >> %DEACT_D%\dotnet_home.cmd

@REM -- pwsh activation/deactivation --
echo $Env:_CONDA_PKG_BACKUP_PATH = "$Env:PATH"; >> %ACT_D%\dotnet_home.ps1
echo $Env:_CONDA_PKG_BACKUP_DOTNET_HOME = "$Env:DOTNET_HOME"; >> %ACT_D%\dotnet_home.ps1
echo $Env:DOTNET_HOME = "%DOTNET_HOME%"; >> %ACT_D%\dotnet_home.ps1
echo $Env:PATH = "%PATH%;%DOTNET_HOME%"; >> %ACT_D%\dotnet_home.ps1
echo $Env:PATH = "$Env:_CONDA_PKG_BACKUP_PATH"; >> %DEACT_D%\dotnet_home.ps1
echo $Env:DOTNET_HOME = "$Env:_CONDA_PKG_BACKUP_DOTNET_HOME"; >> %DEACT_D%\dotnet_home.ps1

@REM -- posix-style activation/deactivation --
echo export _CONDA_PKG_BACKUP_PATH="$PATH"; >> %ACT_D%\dotnet_home.sh
echo export _CONDA_PKG_BACKUP_DOTNET_HOME="$DOTNET_HOME"; >> %ACT_D%\dotnet_home.sh
echo export DOTNET_HOME="%DOTNET_HOME%"; >> %ACT_D%\dotnet_home.sh
echo export PATH="%PATH%;%DOTNET_HOME%"; >> %ACT_D%\dotnet_home.sh
echo export PATH="$_CONDA_PKG_BACKUP_PATH" >> %DEACT_D%\dotnet_home.sh
echo export DOTNET_HOME="$_CONDA_PKG_BACKUP_DOTNET_HOME" >> %DEACT_D%\dotnet_home.sh
