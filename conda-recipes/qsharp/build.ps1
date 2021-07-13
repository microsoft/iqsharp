# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$Env:QSHARP_PY_ISCONDA = "True";
Push-Location src/drops/wheels
    Get-ChildItem *.whl `
    | ForEach-Object { pip install --no-index --find-links . --prefix $Env:PREFIX $_.Name }
Pop-Location
