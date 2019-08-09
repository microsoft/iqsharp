DOTNET_ROOT=$PREFIX/opt/dotnet
# The dotnet-install script uses non-POSIX compilant set commands, so we need
# to explicitly use bash.
/bin/bash ./dotnet-install.sh -Version $PKG_VERSION -NoPath -InstallDir $DOTNET_ROOT

# We can save environment variables into the new package using the technique
# demonstrated at:
#     https://github.com/conda-forge/staged-recipes/pull/2002/files
# We do so here to ensure that the new install of .NET Core SDK is added
# to %PATH%, and that the %DOTNET_ROOT% variable is set for use with global
# tools.

# Since the activate.d mechanism is shell-dependent, we will also need to
# provide activation and deactivation scripts for PowerShell and
# for POSIX-style shells.

# In the future, we should transition this mechanism over to use what is
# implemented to resolve the shell-dependence of the current activate.d
# system. See also:
#     https://github.com/conda/conda/issues/6820.

ACT_D=$PREFIX/etc/conda/activate.d
mkdir -p $ACT_D
DEACT_D=$PREFIX/etc/conda/deactivate.d
mkdir -p $DEACT_D

# -- pwsh activation/deactivation --
echo "\$Env:_CONDA_PKG_BACKUP_PATH = \"\$Env:PATH\";" >> $ACT_D/dotnet_root.ps1
echo "\$Env:_CONDA_PKG_BACKUP_DOTNET_ROOT = \"\$Env:DOTNET_ROOT\";" >> $ACT_D/dotnet_root.ps1
echo "\$Env:DOTNET_ROOT = \"$PREFIX/opt/dotnet/\";" >> $ACT_D/dotnet_root.ps1
echo "\$Env:PATH = \"$DOTNET_ROOT:\$Env:PATH\";" >> $ACT_D/dotnet_root.ps1
echo "\$Env:PATH = \"\$Env:_CONDA_PKG_BACKUP_PATH\";" >> $DEACT_D/dotnet_root.ps1
echo "\$Env:DOTNET_ROOT = \"\$Env:_CONDA_PKG_BACKUP_DOTNET_ROOT\";" >> $DEACT_D/dotnet_root.ps1

# -- posix-style activation/deactivation --
echo "export _CONDA_PKG_BACKUP_PATH=\"\$PATH\";" >> $ACT_D/dotnet_root.sh
echo "export _CONDA_PKG_BACKUP_DOTNET_ROOT=\"$DOTNET_ROOT\";" >> $ACT_D/dotnet_root.sh
echo "export DOTNET_ROOT=\"$DOTNET_ROOT\";" >> $ACT_D/dotnet_root.sh
echo "export PATH=\"$DOTNET_ROOT:\$PATH\";" >> $ACT_D/dotnet_root.sh
echo "export PATH=\"\$_CONDA_PKG_BACKUP_PATH\";" >> $DEACT_D/dotnet_root.sh
echo "export DOTNET_ROOT=\"\$_CONDA_PKG_BACKUP_DOTNET_ROOT\";" >> $DEACT_D/dotnet_root.sh
