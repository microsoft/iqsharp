# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
BeforeAll {
    function Test-Environment {
        $Env:AZUREQUANTUM_SUBSCRIPTION_ID | Should -Not -BeNullOrEmpty
        $Env:AZUREQUANTUM_WORKSPACE_RG | Should -Not -BeNullOrEmpty
        $Env:AZUREQUANTUM_WORKSPACE_LOCATION | Should -Not -BeNullOrEmpty
        $Env:AZUREQUANTUM_WORKSPACE_NAME | Should -Not -BeNullOrEmpty
    }
}
    

Describe "Test Jupyter Notebooks" {
    BeforeAll { Test-Environment }

    $Notebooks = Get-ChildItem -Directory -Path .\Notebooks\

    Context "Applying nbconvert to <_>" -ForEach $Notebooks {
        BeforeAll { Push-Location $_ }

        It "Converts Notebook.ipynb in <_> successfully" {
            if (Test-Path "obj") {
                Remove-Item obj -Recurse
            }
            jupyter nbconvert Notebook.ipynb --execute --stdout --to html --ExecutePreprocessor.timeout=120
            $LASTEXITCODE | Should -Be 0
        }
        
        AfterAll { Pop-Location }
    }
}

Describe "Test Python Integration" {

    BeforeAll { 
        Test-Environment
        Push-Location Python
        
        if (Test-Path "obj") {
            Remove-Item obj -Recurse
        }
    }

    It "Runs pytest successfully" {
        python -m pytest --junitxml=junit/TestResults.xml
        $LASTEXITCODE | Should -Be 0
    }
    
    AfterAll { Pop-Location }
}

