# We need to set the PYTHON_VERSION environment variable
# explicitly here, since conda-build doesn't by default pass
# environment variables from the host environment.
$Env:PYTHON_VERSION = $Env:PKG_VERSION
Push-Location src/src/Python/
    python setup.py install --prefix $Env:PREFIX
Pop-Location
