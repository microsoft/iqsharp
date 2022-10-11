# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeAll {
    # Makes sure all the required environment variables are set.
    function Test-Environment {
        $Env:AZURE_QUANTUM_SUBSCRIPTION_ID | Should -Not -BeNullOrEmpty
        $Env:AZURE_QUANTUM_WORKSPACE_RG | Should -Not -BeNullOrEmpty
        $Env:AZURE_QUANTUM_WORKSPACE_LOCATION | Should -Not -BeNullOrEmpty
        $Env:AZURE_QUANTUM_WORKSPACE_NAME | Should -Not -BeNullOrEmpty
        
        # These are needed for environment credentials:
        $Env:AZURE_TENANT_ID | Should -Not -BeNullOrEmpty
        # $Env:AZURE_CLIENT_ID | Should -Not -BeNullOrEmpty
        # $Env:AZURE_CLIENT_SECRET | Should -Not -BeNullOrEmpty
    }

    function Test-Notebook([string]$notebook) {
        if (Test-Path "obj") {
            Remove-Item obj -Recurse
        }

        "Running jupyter nbconvert on '$notebook'" | Write-Verbose
        jupyter nbconvert $notebook --execute --stdout --to markdown --ExecutePreprocessor.timeout=120  | Write-Verbose

        $LASTEXITCODE | Should -Be 0
    }
}

Describe "Test Jupyter Notebooks" {
    BeforeAll { 
        Test-Environment
        Push-Location .\Notebooks
    }

    It "Converts IonQ.ipynb successfully" -Tag "submit.ionq" {
       Test-Notebook "IonQ.ipynb"
    }

    It "Converts ResourceEstimator.ipynb successfully" -Tag "submit.microsoft-qc" {
       Test-Notebook "ResourceEstimator.ipynb"
    }
    
    AfterAll { Pop-Location }
}

Describe "Test Python Integration" {
    BeforeAll { 
        Test-Environment
        Push-Location .\Python

        if (Test-Path "obj") {
            Remove-Item obj -Recurse
        }
    }

    It "Runs pytest successfully for ionq" -Tag "submit.ionq" {
        python -m pytest -k ionq --junitxml="junit/TestResults-IonQ.xml" | Write-Verbose
        $LASTEXITCODE | Should -Be 0
    }

    It "Runs pytest successfully for honeywell" -Tag "submit.honeywell" {
        python -m pytest -k honeywell --junitxml="junit/TestResults-Honeywell.xml" | Write-Verbose
        $LASTEXITCODE | Should -Be 0
    }

    It "Runs pytest successfully for Quantinuum" -Tag "submit.quantinuum" {
        python -m pytest -k quantinuum --junitxml="junit/Quantinuum.xml" | Write-Verbose
        $LASTEXITCODE | Should -Be 0
    }

    It "Runs pytest successfully for estimator" -Tag "submit.microsoft-qc" {
        python -m pytest -k estimator --junitxml="junit/TestResults-Estimator.xml" | Write-Verbose
        $LASTEXITCODE | Should -Be 0
    }
    
    AfterAll { Pop-Location }
}

