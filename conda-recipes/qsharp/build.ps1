# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param(
    [string]
    $Version
)

$versionPyContents = @"
# Auto-generated file, do not edit.
##
# version.py: Specifies the version of the qsharp package.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##
__version__ = "$Version"
is_conda = True
_user_agent_extra = "[$Version](qsharp:conda)"
"@;

Push-Location src/drops/wheels
    # Patch the qsharp-core wheel to add version info.
    Get-ChildItem qsharp_core-*.whl `
        | ForEach-Object {
            Write-Host "##[debug]Patching wheel at $_.";
            wheel unpack $_ --dest unpacked
            $versionPyPath = Get-Item (Join-Path "." "unpacked" "qsharp_core-$Version" "qsharp" "version.py");
            Write-Host "Setting contents of ${versionPyPath}:`n$versionPyContents";
            Set-Content -Path $versionPyPath -Value $versionPyContents -Encoding utf8NoBOM;
            wheel pack (Join-Path "." "unpacked" "qsharp_core-$Version");
        }

    # Install all the wheels, including the wheel we patched above.
    Get-ChildItem *.whl `
        | ForEach-Object {
            pip install --verbose --verbose --no-index --find-links . --prefix $Env:PREFIX $_.Name
        }
Pop-Location
