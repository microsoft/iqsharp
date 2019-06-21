# Microsoft Quantum Development Kit: IQ# Kernel #

Welcome to the Microsoft Quantum Development Kit!

This repository contains the IQ# kernel for the [Quantum Development Kit](https://docs.microsoft.com/quantum/).
This kernel provides Q# support for the Jupyter platform, as well as the backend used by the Python client for Q#.

- **[Jupyter/](./tree/master/Jupyter/)**: Assembly used to interoperate between Jupyter and the rest of the IQ# kernel.
- **[Src/](./tree/master/Src/)**: Core of the IQ# kernel.
- **[Tests/](./tree/master/Tests/)**: Unit tests for IQ#.
- **[Tool/](./tree/master/Tool/)**: .NET Core Global Tool used to install and launch IQ#.
- **[Web/](./tree/master/Web/)**: Provides a RESTful API into IQ#.

## New to Quantum? ##

See the [introduction to quantum computing](https://docs.microsoft.com/quantum/concepts/) provided with the Quantum Development Kit.

## Getting Started ##

The libraries provided in this repository are built using [.NET Core](https://docs.microsoft.com/dotnet/core/) and the
compiler infrastructure provided with the [Quantum Development Kit](https://docs.microsoft.com/quantum/).
Please see the [installation guide](https://docs.microsoft.com/quantum/install-guide) for how to get up and running.

You may also visit our [Quantum](https://github.com/microsoft/quantum) repository, which offers a wide variety
of samples on how to use these libraries to write quantum based programs.

### Building IQ# ###

To build IQ# from Visual Studio 2017 or later, please use the [`iqsharp.sln`](./blob/master/iqsharp.sln) solution file.
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

## Build Status ##

| branch | status    |
|--------|-----------|
| master | [![Build Status](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_apis/build/status/microsoft.iqsharp?branchName=master)](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_build/latest?definitionId=14&branchName=master) |

## Feedback ##

If you have feedback about IQ#, please let us know by filing a [new issue](https://github.com/microsoft/iqsharp/issues/new)!
If you have feedback about some other part of the Microsoft Quantum Development Kit, please see the [contribution guide](https://docs.microsoft.com/quantum/contributing/) for more information.

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

For more details, please see [CONTRIBUTING.md](./tree/master/CONTRIBUTING.md), or the [contribution guide](https://docs.microsoft.com/quantum/contributing/).
