# Microsoft Quantum Development Kit: IQ# Kernel #

Welcome to the Microsoft Quantum Development Kit!

This repository contains the IQ# kernel for the [Quantum Development Kit](https://docs.microsoft.com/azure/quantum/).
This kernel provides Q# support for the Jupyter platform, as well as the backend used by the Python client for Q#.

- **[src/Core/](./src/Core/)**: Core of the IQ# kernel.
- **[src/Kernel/](./src/Kernel/)**: Assembly used to interoperate between Jupyter and the rest of the IQ# kernel.
- **[src/Python/](./src/Python)**: Python package for accessing IQ#.
- **[src/Tests/](./src/Tests/)**: Unit tests for IQ#.
- **[src/Tool/](./src/Tool/)**: .NET Core Global Tool used to install and launch IQ#.
- **[src/Web/](./src/Web/)**: Provides a RESTful API into IQ#.

## New to Quantum? ##

See the [introduction to quantum computing](https://docs.microsoft.com/azure/quantum/concepts-overview) provided with the Quantum Development Kit.

## Getting Started ##

The Jupyter kernel provided in this repository is built using [.NET Core](https://docs.microsoft.com/dotnet/core/) (2.2 or later) and the compiler infrastructure provided with the [Quantum Development Kit](https://docs.microsoft.com/azure/quantum).
Please see the [getting started guide](https://docs.microsoft.com/azure/quantum/install-overview-qdk) for how to get up and running.

You may also visit the [**microsoft/quantum**](https://github.com/microsoft/quantum) repository, which offers a wide variety
of samples on how to use this kernel to run Q# in Jupyter Notebooks, or from Python.

### Building IQ# from Source ###

To obtain prerequisites, ensure that [Node.js](https://nodejs.org/) is installed, and then run `npm install` from the [src/Kernel/](./src/Kernel/) folder:

```
cd src/Kernel/
npm install
```

To build IQ# from Visual Studio 2017 or later, please use the [`iqsharp.sln`](./blob/main/iqsharp.sln) solution file.
To build using the .NET Core SDK, please run `dotnet build iqsharp.sln`.

In either case, the IQ# kernel can be installed by using `dotnet run`:

```
cd src/Tool/
dotnet run -- install
```

Optionally, you can install IQ# in _development mode_, which instructs the Jupyter platform to rebuild IQ# whenever a new kernel is started:

```
cd src/Tool/
dotnet run -- install --develop
```

This can cause some issues, especially when running multiple instances of IQ#, such that we recommend against using development mode in general usage.

Note that when building IQ# from source, this repository is configured so that .NET Core will automatically look at the [Quantum Development Kit prerelease feed](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_packaging?_a=feed&feed=alpha) in addition to any other feeds you may have configured.

### Using IQ# as a Container ###

This repository provides a [Dockerfile](./images/iqsharp-base/Dockerfile) that includes the .NET Core SDK, Python, Jupyter Notebook, and the IQ# kernel.

The image built from this Dockerfile is hosted on the [Microsoft Container Registry](https://github.com/microsoft/ContainerRegistry) as the `quantum/iqsharp-base` repository.
The `iqsharp-base` image can be used, for instance, to quickly enable using [Binder](https://gke.mybinder.org/) with Q#-language repositories, or as a base image for [Visual Studio Code Development Containers](https://code.visualstudio.com/docs/remote/containers).

To use the `iqsharp-base` image in your own Dockerfile, make sure to begin your Dockerfile with a `FROM` line that points to the Microsoft Container Registry:

```Dockerfile
FROM mcr.microsoft.com/quantum/iqsharp-base:latest
```

To use the `iqsharp-base` image as a development container for Visual Studio Code, add a [`.devcontainer` folder](https://code.visualstudio.com/docs/remote/containers#_using-an-image-or-dockerfile) that points to the Microsoft Container Registry:

```json
{
    "image": "mcr.microsoft.com/quantum/iqsharp-base:latest",
    "extensions": [
        "quantum.quantum-devkit-vscode",
        "ms-vscode.csharp"
    ]
}
```

In either case, you can also use a Quantum Development Kit version number (0.8 or later) in place of `latest` to point to a specific version.

## Build Status ##

| branch | status    |
|--------|-----------|
| main | [![Build Status](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_apis/build/status/microsoft.iqsharp?branchName=main)](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_build/latest?definitionId=14&branchName=main) |

## Feedback ##

If you have feedback about IQ#, please let us know by filing a [new issue](https://github.com/microsoft/iqsharp/issues/new)!
If you have feedback about some other part of the Microsoft Quantum Development Kit, please see the [contribution guide](https://docs.microsoft.com/en-us/azure/quantum/contributing-overview) for more information.

## Legal and Licensing ##

### Telemetry ###

By default, IQ# collects information about the runtime performance of IQ#.
To opt-out of sending telemetry, create an environment variable called IQSHARP_TELEMETRY_OPT_OUT set to a value of 1 before starting IQ#.
The telemetry we collect falls under the [Microsoft Privacy Statement](https://privacy.microsoft.com/privacystatement).

### Data Collection ###

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft's privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

## Contributing ##

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

For more details, please see [CONTRIBUTING.md](./tree/main/CONTRIBUTING.md), or the [contribution guide](https://docs.microsoft.com/en-us/azure/quantum/contributing-overview).
