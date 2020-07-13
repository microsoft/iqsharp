# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Push-Location src/src/Python/dist
    Get-ChildItem *.whl `
    | ForEach-Object { pip install --no-index --find-links . --prefix $Env:PREFIX $_.Name }
Pop-Location
