# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Push-Location src/drops/wheels
    Get-ChildItem *.whl `
    | ForEach-Object { pip install --no-index --find-links . --prefix $Env:PREFIX $_.Name }
Pop-Location


# Rewrite the version.py file in qsharp-core, adding conda metadata.
$versionPyContents = @"
# Auto-generated file, do not edit.
##
# version.py: Specifies the version of the qsharp package.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##
__version__ = "$Env:PYTHON_VERSION"
is_conda = True
_user_agent_extra = "[$Env:PYTHON_VERSION](qsharp:conda)"
"@;
$versionPyPath = python -c "import qsharp.version; print(qsharp.version.__file__)";
Set-Content -Path $versionPyPath -Value $versionPyContents -Encoding utf8NoBOM;
