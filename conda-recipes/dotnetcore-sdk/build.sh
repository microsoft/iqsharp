DOTNET_HOME=$PREFIX/opt/dotnet
/bin/sh ./dotnet-install.sh -Version $PKG_VERSION -NoPath -InstallDir $DOTNET_HOME

# We can save environment variables into the new package using the technique
# demonstrated at:
#     https://github.com/conda-forge/staged-recipes/pull/2002/files
# We do so here to ensure that the new install of .NET Core SDK is added
# to %PATH%, and that the %DOTNET_HOME% variable is set for use with global
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
echo "\$Env:_CONDA_PKG_BACKUP_PATH = \"\$Env:PATH\";" >> $ACT_D/dotnet_home.ps1
echo "\$Env:_CONDA_PKG_BACKUP_DOTNET_HOME = \"\$Env:DOTNET_HOME\";" >> $ACT_D/dotnet_home.ps1
echo "\$Env:DOTNET_HOME = \"$PREFIX/opt/dotnet/\";" >> $ACT_D/dotnet_home.ps1
echo "\$Env:PATH = \"$PATH:$DOTNET_HOME\";" >> $ACT_D/dotnet_home.ps1
echo "\$Env:PATH = \"\$Env:_CONDA_PKG_BACKUP_PATH\";" >> $DEACT_D/dotnet_home.ps1
echo "\$Env:DOTNET_HOME = \"\$Env:_CONDA_PKG_BACKUP_DOTNET_HOME\";" >> $DEACT_D/dotnet_home.ps1

# -- posix-style activation/deactivation --
echo "export _CONDA_PKG_BACKUP_PATH=\"$PATH\";" >> $ACT_D/dotnet_home.sh
echo "export _CONDA_PKG_BACKUP_DOTNET_HOME=\"$DOTNET_HOME\";" >> $ACT_D/dotnet_home.sh
echo "export DOTNET_HOME=\"$DOTNET_HOME\";" >> $ACT_D/dotnet_home.sh
echo "export PATH=\"$PATH:$DOTNET_HOME\";" >> $ACT_D/dotnet_home.sh
echo "export PATH=\"\$_CONDA_PKG_BACKUP_PATH\";" >> $DEACT_D/dotnet_home.sh
echo "export DOTNET_HOME=\"\$_CONDA_PKG_BACKUP_DOTNET_HOME\";" >> $DEACT_D/dotnet_home.sh
