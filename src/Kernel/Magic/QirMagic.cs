// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using LlvmBindings;
using LlvmBindings.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;

namespace Microsoft.Quantum.IQSharp.Kernel;

public record LlvmIr(
    string Text
) : IDisplayable
{
    public bool TryAsDisplayData(string mimeType, [NotNullWhen(true)] out EncodedData? displayData)
    {
        switch (mimeType)
        {
            case MimeTypes.Html:
                displayData = $"<pre><code>{WebUtility.HtmlEncode(Text)}</code></pre>".ToEncodedData();
                return true;

            case MimeTypes.PlainText:
                displayData = Text.ToEncodedData();
                return true;

            default:
                displayData = null;
                return false;
        }
    }
}

/// <summary>
///     A magic command that can be used to generate QIR from a given
///     operation as an entry point.
/// </summary>
public class QirMagic : AbstractMagic
{
    private const string ParameterNameOperationName = "__operationName__";
    private const string ParameterNameTarget = "target";
    private const string ParameterNameTargetCapability = "target_capability";
    private const string ParameterNameOutputFile = "output_file";
    private const string ParameterNameOutputFormat = "output_format";

    private enum QirOutputFormat
    {
        IR,
        Bitcode,
        BitcodeBase64,
    }

    /// <summary>
    ///     Constructs the magic command from DI services.
    /// </summary>
    public QirMagic(
        ISymbolResolver resolver,
        IEntryPointGenerator entryPointGenerator,
        ILogger<SimulateMagic> logger,
        IAzureClient azureClient,
        ISymbolResolver symbolResolver,
        IConfigurationSource configurationSource,
        IMetadataController metadataController,
        ISnippets snippets
    ) : base(
        "qir",
        new Microsoft.Jupyter.Core.Documentation
        {
            Summary = "Compiles a given Q# entry point to QIR, saving the resulting QIR to a given file.",
            Description = $@"
                This command takes the full name of a Q# entry point, and compiles the Q# from that entry point
                into QIR. The resulting program is then executed, and the output of the program is displayed.

                #### Required parameters

                - `{ParameterNameOperationName}=<string>`: Q# operation or function name.
                This must be the first parameter, and must be a valid Q# operation
                or function name that has been defined either in the notebook or in a Q# file in the same folder.

                #### Optional parameters

                - `{ParameterNameTarget}=<string>`: The intended execution target for the compiled entrypoint.
                Defaults to the active Azure Quantum target (which can be set with `%azure.target`).                
                Otherwise, defaults to a generic target, which may not work when running on a specific target.

                - `{ParameterNameTargetCapability}=<string>`: The capability of the intended execution target.
                If `{ParameterNameTarget}` is specified or there is an active Azure Quantum target,
                defaults to the target's maximum capability.
                Otherwise, defaults to `FullComputation`, which may not be supported when running on a specific target.
                Possible options are:
                    * `{TargetCapabilityModule.Top}`
                    * `{TargetCapabilityModule.Bottom}`
                    * `{TargetCapabilityModule.BasicExecution}`
                    * `{TargetCapabilityModule.AdaptiveExecution}`
                    * `{TargetCapabilityModule.BasicQuantumFunctionality}`
                    * `{TargetCapabilityModule.BasicMeasurementFeedback}`
                    * `{TargetCapabilityModule.FullComputation}`

                - `{ParameterNameOutputFile}=<string>`: The file path for where to save the output QIR.
                If empty, a uniquely-named temporary file will be created.

                - `{ParameterNameOutputFormat}=<QirOutputFormat>`: The QIR output format.
                Defaults to `IR`.
                Possible options are:
                    * `{nameof(QirOutputFormat.IR)}`: Human-readable Intermediate Representation in plain-text
                    * `{nameof(QirOutputFormat.Bitcode)}`: LLVM bitcode (only when writing to a output file)
                    * `{nameof(QirOutputFormat.BitcodeBase64)}`: LLVM bitcode encoded as Base64
            ".Dedent(),
            Examples = new []
            {
                @"
                    Compiles a Q# program to QIR starting at the entry point defined as
                    `operation RunMain() : Result` and display the resulting LLVM IR:
                    ```
                    In []: %qir RunMain
                    ```
                ".Dedent()
            }
        }, logger)
    {
        this.EntryPointGenerator = entryPointGenerator;
        this.Logger = logger;
        this.AzureClient = azureClient;
        this.SymbolResolver = symbolResolver;
        this.ConfigurationSource = configurationSource;
        this.MetadataController = metadataController;
        this.Snippets = snippets;
    }

    private IAzureClient AzureClient { get; }
    private IConfigurationSource ConfigurationSource { get; }
    private ISymbolResolver SymbolResolver { get; }
    private IMetadataController MetadataController { get; }
    private ISnippets Snippets { get; }

    private ILogger? Logger { get; }

    // TODO: EntryPointGenerator might should move out of the Azure Client
    //       project.
    // GitHub Issue: https://github.com/microsoft/iqsharp/issues/610
    private IEntryPointGenerator EntryPointGenerator { get; }

    /// <inheritdoc />
    public override ExecutionResult Run(string input, IChannel channel) =>
        RunAsync(input, channel).Result;

    private static unsafe bool TryParseBitcode(string path, out LLVMModuleRef outModule, out string outMessage)
    {
        LLVMMemoryBufferRef handle;
        sbyte* msg;
        if (LLVM.CreateMemoryBufferWithContentsOfFile(path.AsMarshaledString(), (LLVMOpaqueMemoryBuffer**)&handle, &msg) != 0)
        {
            var span = new ReadOnlySpan<byte>(msg, int.MaxValue);
            var errTxt = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            LLVM.DisposeMessage(msg);
            throw new InternalCodeGeneratorException(errTxt);
        }

        fixed (LLVMModuleRef* pOutModule = &outModule)
        {
            sbyte* pMessage = null;
            var result = LLVM.ParseBitcodeInContext(
                LLVM.ContextCreate(),
                handle,
                (LLVMOpaqueModule**)pOutModule,
                &pMessage);

            if (pMessage == null)
            {
                outMessage = string.Empty;
            }
            else
            {
                var span = new ReadOnlySpan<byte>(pMessage, int.MaxValue);
                outMessage = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            return result == 0;
        }
    }

    /// <summary>
    ///     Simulates an operation given a string with its name and a JSON
    ///     encoding of its arguments.
    /// </summary>
    public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
    {
        var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameOperationName);

        var name = inputParameters.DecodeParameter<string>(ParameterNameOperationName);
        if (name == null) throw new InvalidOperationException($"No operation name provided.");

        var symbol = SymbolResolver.Resolve(name) as IQSharpSymbol;
        if (symbol == null)
        {
            new CommonMessages.NoSuchOperation(name).Report(channel, ConfigurationSource);
            return ExecuteStatus.Error.ToExecutionResult();
        }

        var outputFilePath = inputParameters.DecodeParameter<string>(ParameterNameOutputFile);
        var outputFormat = inputParameters.DecodeParameter<QirOutputFormat>(ParameterNameOutputFormat,
                                                                            QirOutputFormat.IR);
        var target = inputParameters.DecodeParameter<string>(ParameterNameTarget,
                                                             this.AzureClient.ActiveTarget?.TargetId);
        var capabilityName = inputParameters.DecodeParameter<string>(ParameterNameTargetCapability);
        var capability = !string.IsNullOrEmpty(capabilityName) 
                         ? TargetCapabilityModule.FromName(capabilityName).Value
                         : AzureExecutionTarget.GetMaximumCapability(target);
        
        IEntryPoint entryPoint;
        try
        {
            entryPoint = await EntryPointGenerator.Generate(name, target, capability, generateQir: true);
        }
        catch (TaskCanceledException tce)
        {
            throw tce;
        }
        catch (CompilationErrorsException exception)
        {
            return ReturnCompilationError(input, channel, name, exception);
        }

        if (entryPoint is null)
        {
            return "Internal error: generated entry point was null, but no compilation errors were returned."
                .ToExecutionResult(ExecuteStatus.Error);
        }

        if (entryPoint.QirStream is null)
        {
            return "Internal error: generated entry point does not contain a QIR bitcode stream, but no compilation errors were returned."
                .ToExecutionResult(ExecuteStatus.Error);
        }

        Stream qriStream = entryPoint.QirStream;

        return outputFormat switch
        {
            QirOutputFormat.Bitcode =>
                ReturnQirBitcode(outputFilePath, outputFormat, qriStream),
            QirOutputFormat.BitcodeBase64 =>
                ReturnQirBitcodeBase64(outputFilePath, outputFormat, qriStream),
            QirOutputFormat.IR or _ =>
                ReturnQIR_IR(channel, outputFilePath, outputFormat, qriStream),
        };
    }

    private ExecutionResult ReturnQIR_IR(IChannel channel, string? outputFilePath, QirOutputFormat outputFormat, Stream qirStream)
    {
        string bitcodeFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".bc")!;
        string irFilePath = string.IsNullOrEmpty(outputFilePath)
                            ? Path.ChangeExtension(bitcodeFilePath, ".ll")
                            : outputFilePath;

        using (var outStream = File.OpenWrite(bitcodeFilePath))
        {
            qirStream.CopyTo(outStream);
        }

        if (!TryParseBitcode(bitcodeFilePath, out var moduleRef, out var parseErr))
        {
            var msg = $"Internal error: Could not parse generated QIR bitcode.\nLLVM returned error message: {parseErr}";
            channel.Stderr(msg);
            Logger.LogError(msg);
            return ExecuteStatus.Error.ToExecutionResult();
        }

        if (!moduleRef.TryPrintToFile(irFilePath, out var writeErr))
        {
            return $"Error generating IR from bitcode: {writeErr}"
                   .ToExecutionResult(ExecuteStatus.Error);
        }

        if (!string.IsNullOrEmpty(outputFilePath))
        {
            return new LlvmIr($"QIR {outputFormat} written to {outputFilePath}").ToExecutionResult();
        }

        var llvmIR = File.ReadAllText(irFilePath);
        return new LlvmIr(llvmIR).ToExecutionResult();
    }

    private static ExecutionResult ReturnQirBitcodeBase64(string? outputFilePath, QirOutputFormat outputFormat, Stream qirStream)
    {
        byte[] bytes;
        using (var memoryStream = new MemoryStream())
        {
            qirStream.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }
        string bitchedBase64 = System.Convert.ToBase64String(bytes);

        if (!string.IsNullOrEmpty(outputFilePath))
        {
            File.WriteAllText(outputFilePath, bitchedBase64);
            return new LlvmIr($"QIR {outputFormat} written to {outputFilePath}").ToExecutionResult();
        }

        return new LlvmIr(bitchedBase64).ToExecutionResult();
    }

    private static ExecutionResult ReturnQirBitcode(string? outputFilePath, QirOutputFormat outputFormat, Stream qirStream)
    {
        if (string.IsNullOrEmpty(outputFilePath))
        {
            return $"Bitcode format can only be written to a file. You must pass the `{ParameterNameOutputFile}` parameter."
                   .ToExecutionResult(ExecuteStatus.Error);
        }

        using (var outStream = File.OpenWrite(outputFilePath))
        {
            qirStream.CopyTo(outStream);
        }

        return new LlvmIr($"QIR {outputFormat} written to {outputFilePath}").ToExecutionResult();
    }

    private ExecutionResult ReturnCompilationError(string input, IChannel channel, string? name, CompilationErrorsException exception)
    {
        var msg = $"The Q# operation {name} could not be compiled as an entry point for job execution.";
        this.Logger?.LogError(exception, msg);
        channel.Stderr(msg);
        channel.Stderr(exception.Message);

        if (MetadataController.IsPythonUserAgent() || ConfigurationSource.CompilationErrorStyle == CompilationErrorStyle.Basic)
        {
            foreach (var m in exception.Errors) channel.Stderr(m);
        }
        else
        {
            channel.DisplayFancyDiagnostics(exception.Diagnostics, Snippets, input);
        }

        return AzureClientError.InvalidEntryPoint.ToExecutionResult();
    }
}
