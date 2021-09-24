# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeAll {
    # Makes sure all the required environment variables are set.
    function Test-Environment {
        $Env:AZURE_QUANTUM_SUBSCRIPTION_ID | Should -Not -BeNullOrEmpty
        $Env:AZURE_QUANTUM_WORKSPACE_RG | Should -Not -BeNullOrEmpty
        $Env:AZURE_QUANTUM_WORKSPACE_LOCATION | Should -Not -BeNullOrEmpty
        $Env:AZURE_QUANTUM_WORKSPACE_NAME | Should -Not -BeNullOrEmpty
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

