set DOTNET_ROOT=%PREFIX%\opt\dotnet

@REM We use powershell.exe and not pwsh.exe to invoke the downloaded script,
@REM as we do not know whether or not the build agent has PowerShell 6 or later
@REM installed.
powershell.exe -NoProfile -Command "./dotnet-install.ps1 -Version %PKG_VERSION% -NoPath -InstallDir %DOTNET_ROOT%"

@REM We can save environment variables into the new package using the technique
@REM demonstrated at:
@REM     https://github.com/conda-forge/staged-recipes/pull/2002/files
@REM We do so here to ensure that the new install of .NET Core SDK is added
@REM to %PATH%, and that the %DOTNET_ROOT% variable is set for use with global
@REM tools.

@REM Since the activate.d mechanism is shell-dependent, we will also need to
@REM provide activation and deactivation scripts for cmd.exe, PowerShell, and
@REM for POSIX-style shells.

@REM In the future, we should transition this mechanism over to use what is
@REM implemented to resolve the shell-dependence of the current activate.d
@REM system. See also:
@REM     https://github.com/conda/conda/issues/6820.

@REM Known issues:
@REM - Does not support recursive entry into environments, as the
@REM   _CONDA_PKG_BACKUP_* variables are overwritten by activate.d calls.
@REM - POSIX-style shells are not yet supported on Windows, as it is not clear
@REM   how to convert Windows paths visible to cmd.exe to a format expected by
@REM   Git Bash or msys2 bash.

set ACT_D=%PREFIX%\etc\conda\activate.d
md %ACT_D%
set DEACT_D=%PREFIX%\etc\conda\deactivate.d
md %DEACT_D%

@REM -- cmd activation/deactivation --
echo set _CONDA_PKG_BACKUP_PATH=%%PATH%% >> %ACT_D%\dotnet_root.cmd
echo set _CONDA_PKG_BACKUP_DOTNET_ROOT=%%DOTNET_ROOT%% >> %ACT_D%\dotnet_root.cmd
echo set DOTNET_ROOT=%DOTNET_ROOT% >> %ACT_D%\dotnet_root.cmd
echo set PATH=%DOTNET_ROOT%;%%PATH%% >> %ACT_D%\dotnet_root.cmd
echo set PATH=%%_CONDA_PKG_BACKUP_PATH%% >> %DEACT_D%\dotnet_root.cmd
echo set DOTNET_ROOT=%%_CONDA_PKG_BACKUP_DOTNET_ROOT%% >> %DEACT_D%\dotnet_root.cmd
echo set _CONDA_PKG_BACKUP_DOTNET_ROOT= >> %DEACT_D%\dotnet_root.cmd
echo set _CONDA_PKG_BACKUP_PATH= >> %DEACT_D%\dotnet_root.cmd

@REM -- pwsh activation/deactivation --
echo $Env:_CONDA_PKG_BACKUP_PATH = "$Env:PATH"; >> %ACT_D%\dotnet_root.ps1
echo $Env:_CONDA_PKG_BACKUP_DOTNET_ROOT = "$Env:DOTNET_ROOT"; >> %ACT_D%\dotnet_root.ps1
echo $Env:DOTNET_ROOT = "%DOTNET_ROOT%"; >> %ACT_D%\dotnet_root.ps1
echo $Env:PATH = "%DOTNET_ROOT%;$Env:PATH"; >> %ACT_D%\dotnet_root.ps1
echo $Env:PATH = "$Env:_CONDA_PKG_BACKUP_PATH"; >> %DEACT_D%\dotnet_root.ps1
echo $Env:DOTNET_ROOT = "$Env:_CONDA_PKG_BACKUP_DOTNET_ROOT"; >> %DEACT_D%\dotnet_root.ps1
echo $Env:_CONDA_PKG_BACKUP_PATH = $null; >> %DEACT_D%\dotnet_root.ps1
echo $Env:_CONDA_PKG_BACKUP_DOTNET_ROOT = $null; >> %DEACT_D%\dotnet_root.ps1

@REM -- posix-style activation/deactivation --
@REM echo export _CONDA_PKG_BACKUP_PATH="$PATH"; >> %ACT_D%\dotnet_root.sh
@REM echo export _CONDA_PKG_BACKUP_DOTNET_ROOT="$DOTNET_ROOT"; >> %ACT_D%\dotnet_root.sh
@REM echo export DOTNET_ROOT="%DOTNET_ROOT%"; >> %ACT_D%\dotnet_root.sh
@REM echo export PATH="$PATH;%DOTNET_ROOT%"; >> %ACT_D%\dotnet_root.sh
@REM echo export PATH="$_CONDA_PKG_BACKUP_PATH" >> %DEACT_D%\dotnet_root.sh
@REM echo export DOTNET_ROOT="$_CONDA_PKG_BACKUP_DOTNET_ROOT" >> %DEACT_D%\dotnet_root.sh
@REM echo unset _CONDA_PKG_BACKUP_DOTNET_ROOT >> %DEACT_D%\dotnet_root.sh
@REM echo unset _CONDA_PKG_BACKUP_PATH >> %DEACT_D%\dotnet_root.sh
