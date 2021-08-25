# IQ# Live tests

This folder contains Live tests for IQ#.
Live tests are end-to-end tests that require an actual connection with Azure Quantum
to complete successfully.

Notice these tests are currently not part of IQ#'s CI pipeline.
Please run them manually if making Azure Quantum related changes.

## Running locally

To run these tests:

1. Use [Install-Artifacts.ps1](./Install-Artifacts.ps1) to install IQ#, either from
   from the local [`/src`](../src) folder, or from build artifacts by specifying the following
   environment variables:
    * `NUGET_OUTDIR`: with the location of the build's NuGet packages
    * `NUGET_VERSION`: with the packages' NuGet version.
    * `PYTHON_OUTDIR`: with the location of the build's Python wheels
2. Login to Azure using either:
    * the [Azure Account extension in VS Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.azure-account)
    * `az login` from the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/)
3. Set up the following environment variables pointing to an Azure Quantum Workspace that has the Microsoft, Ionq and Honeywell providers enabled:
    * `$Env:AZUREQUANTUM_SUBSCRIPTION_ID=""`
    * `$Env:AZUREQUANTUM_WORKSPACE_RG=""`
    * `$Env:AZUREQUANTUM_WORKSPACE_LOCATION=""`
    * `$Env:AZUREQUANTUM_WORKSPACE_NAME=""`
4. Use [Run.ps1](.\Run.ps1) to run all the tests.
