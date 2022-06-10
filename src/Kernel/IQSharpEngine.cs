// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Immutable;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.QsCompiler.BondSchemas;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///     Arguments for the <see cref="CompletionEvent"/> event.
    /// </summary>
    public class CompletionEventArgs
    {
        /// <summary>
        ///     The number of completions returned by the event.
        /// </summary>
        public int NCompletions { get; set; }

        /// <summary>
        ///      The time taken to respond to the completion request.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    ///      An event raised when completions are provided in response to a
    ///      completion request.
    /// </summary>
    public record CompletionEvent : Event<CompletionEventArgs>;

    /// <summary>
    ///  The IQsharpEngine, used to expose Q# as a Jupyter kernel.
    /// </summary>
    public class IQSharpEngine : BaseEngine
    {
        private readonly IPerformanceMonitor performanceMonitor;
        private readonly IConfigurationSource configurationSource;
        private readonly IServiceProvider services;
        private readonly ILogger<IQSharpEngine> logger;
        private readonly IMetadataController metadataController;
        private readonly ICommsRouter commsRouter;
        private readonly IEventService eventService;

        // NB: These properties may be null if the engine has not fully started
        //     up yet.
        internal ISnippets? Snippets { get; private set; } = null;

        internal ISymbolResolver? SymbolsResolver { get; private set; } = null;

        internal IWorkspace? Workspace { get; private set; } = null;

        private TaskCompletionSource<bool> initializedSource = new TaskCompletionSource<bool>();

        /// <summary>
        ///     Internal-only method for getting services used by this engine.
        ///     Mainly useful in unit tests, where internal state of the
        ///     engine may need to be tested to properly mock communications
        ///     with Azure services.
        /// </summary>
        internal async Task<TService> GetEngineService<TService>() =>
            await services.GetRequiredServiceInBackground<TService>();


        /// <inheritdoc />
        public override Task Initialized => initializedSource.Task;

        /// <summary>
        /// The main constructor. It expects an `ISnippets` instance that takes care
        /// of compiling and keeping track of the code Snippets provided by users.
        /// </summary>
        public IQSharpEngine(
            IShellServer shell,
            IOptions<KernelContext> context,
            ILogger<IQSharpEngine> logger,
            IServiceProvider services,
            IConfigurationSource configurationSource,
            IPerformanceMonitor performanceMonitor,
            IShellRouter shellRouter,
            IMetadataController metadataController,
            ICommsRouter commsRouter,
            IEventService eventService
        ) : base(shell, shellRouter, context, logger, services)
        {
            this.performanceMonitor = performanceMonitor;
            performanceMonitor.EnableBackgroundReporting = true;
            performanceMonitor.OnKernelPerformanceAvailable += (source, args) =>
            {
                logger.LogInformation(
                    "Estimated RAM usage:" +
                    "\n\tManaged: {Managed} bytes" +
                    "\n\tTotal:   {Total} bytes",
                    args.ManagedRamUsed,
                    args.TotalRamUsed
                );
            };
            performanceMonitor.Start();
            this.configurationSource = configurationSource;
            this.services = services;
            this.logger = logger;
            this.metadataController = metadataController;
            this.commsRouter = commsRouter;
            this.eventService = eventService;

            // Start comms routers as soon as possible, so that they can
            // be responsive during kernel startup.
            this.AttachCommsListeners();
        }

        /// <inheritdoc />
        public override void Start() =>
            this.StartAsync().Wait();

        /// <summary>
        ///     Attaches events to listen to comm_open messages from the
        ///     client.
        /// </summary>
        private void AttachCommsListeners()
        {
            // Make sure that the constructor for the iqsharp_clientinfo
            // comms message is called.
            services.GetRequiredService<ClientInfoListener>();

            // Handle a simple comm session handler for echo messages.
            commsRouter.SessionOpenEvent("iqsharp_echo").On += (session, data) =>
            {
                session.OnMessage += async (content) =>
                {
                    if (content.RawData.TryAs<string>(out var data))
                    {
                        await session.SendMessage(data);
                    }
                    await session.Close();
                };
                // We don't have anything meaningful to wait on, so just return
                // a complete task.
                return Task.CompletedTask;
            };
        }

        private async Task StartAsync()
        {
            base.Start();
            var eventService = services.GetRequiredService<IEventService>();
            eventService.Events<WorkspaceReadyEvent, IWorkspace>().On += (workspace) =>
            {
                logger?.LogInformation(
                    "Workspace ready {Time} after startup.",
                    DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
                );
            };

            // Start registering magic symbols; we do this in the engine rather
            // than in the kernel startup event so that we can make sure to
            // gate any magic execution on having added relevant magic symbols.
            services.AddBuiltInMagicSymbols();

            // Start looking for magic symbols in the background while
            // completing other initialization tasks; we'll await at the end.
            var magicSymbolsDiscovered = Task.Run(() =>
            {
                (
                    services.GetRequiredService<IMagicSymbolResolver>() as IMagicSymbolResolver
                )?.FindAllMagicSymbols();
            });

            // Before anything else, make sure to start the right background
            // thread on the Q# compilation loader to initialize serializers
            // and deserializers. Since that runs in the background, starting
            // the engine should not be blocked, and other services can
            // continue to initialize while the Q# compilation loader works.
            //
            // For more details, see:
            //     https://github.com/microsoft/qsharp-compiler/pull/727
            //     https://github.com/microsoft/qsharp-compiler/pull/810
            logger.LogDebug("Loading serialization and deserialziation protocols.");
            Protocols.Initialize();

            logger.LogDebug("Getting services required to start IQ# engine.");
            var serviceTasks = new
            {
                Snippets = services.GetRequiredServiceInBackground<ISnippets>(logger),
                SymbolsResolver = services.GetRequiredServiceInBackground<ISymbolResolver>(logger),
                MagicResolver = services.GetRequiredServiceInBackground<IMagicSymbolResolver>(logger),
                Workspace = services.GetRequiredServiceInBackground<IWorkspace>(logger),
                References = services.GetRequiredServiceInBackground<IReferences>(logger)
            };

            this.Snippets = await serviceTasks.Snippets;
            this.SymbolsResolver = await serviceTasks.SymbolsResolver;
            this.MagicResolver = await serviceTasks.MagicResolver;
            this.Workspace = await serviceTasks.Workspace;
            var references = await serviceTasks.References;

            logger.LogDebug("Registering IQ# display and JSON encoders.");
            RegisterDisplayEncoder<IQSharpSymbolToHtmlResultEncoder>();
            RegisterDisplayEncoder<IQSharpSymbolToTextResultEncoder>();
            RegisterDisplayEncoder<TaskStatusToTextEncoder>();
            RegisterDisplayEncoder<StateVectorToHtmlResultEncoder>();
            RegisterDisplayEncoder<StateVectorToTextResultEncoder>();
            RegisterDisplayEncoder<DataTableToHtmlEncoder>();
            RegisterDisplayEncoder<DataTableToTextEncoder>();
            RegisterDisplayEncoder<DisplayableExceptionToHtmlEncoder>();
            RegisterDisplayEncoder<DisplayableExceptionToTextEncoder>();
            RegisterDisplayEncoder<DisplayableHtmlElementEncoder>();
            RegisterDisplayEncoder<TaskProgressToHtmlEncoder>();
            RegisterDisplayEncoder<TargetCapabilityToHtmlEncoder>();
            RegisterDisplayEncoder<FancyErrorToTextEncoder>();
            RegisterDisplayEncoder<FancyErrorToHtmlEncoder>();

            // For back-compat with older versions of qsharp.py <= 0.17.2105.144881
            // that expected the application/json MIME type for the JSON data.
            var userAgentVersion = metadataController.GetUserAgentVersion();
            logger.LogInformation($"userAgentVersion: {userAgentVersion}");
            var jsonMimeType = metadataController?.UserAgent?.StartsWith("qsharp.py") == true
                ? userAgentVersion != null && userAgentVersion > new Version(0, 17, 2105, 144881)
                    ? "application/x-qsharp-data"
                    : "application/json"
                : "application/x-qsharp-data";

            // Register JSON encoders, and make sure that Newtonsoft.Json
            // doesn't throw exceptions on reference loops.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            RegisterJsonEncoder(jsonMimeType,
                JsonConverters.AllConverters
                .Concat(AzureClient.JsonConverters.AllConverters)
                .ToArray());

            logger.LogDebug("Registering IQ# symbol resolvers.");
            RegisterSymbolResolver(this.SymbolsResolver);
            RegisterSymbolResolver(this.MagicResolver);

            logger.LogDebug("Loading known assemblies and registering package loading.");
            RegisterPackageLoadedEvent(services, logger, references);

            // Handle new shell messages.
            ShellRouter.RegisterHandlers<IQSharpEngine>();

            // Report performance after completing startup.
            performanceMonitor.Report();
            logger.LogInformation(
                "IQ# engine started successfully as process {Process}.",
                Process.GetCurrentProcess().Id
            );

            await magicSymbolsDiscovered;
            eventService?.TriggerServiceInitialized<IExecutionEngine>(this);

            var initializedSuccessfully = initializedSource.TrySetResult(true);
            #if DEBUG
                Debug.Assert(initializedSuccessfully, "Was unable to complete initialization task.");
            #endif
        }

        internal void RegisterDisplayEncoder<T>()
        where T: IResultEncoder =>
            RegisterDisplayEncoder(ActivatorUtilities.CreateInstance<T>(services));

        /// <inheritdoc />
        public override async Task<CompletionResult?> Complete(string code, int cursorPos)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var completions = await base.Complete(code, cursorPos);
            stopwatch.Stop();
            eventService.Trigger<CompletionEvent, CompletionEventArgs>(new CompletionEventArgs
            {
                NCompletions = completions?.Matches?.Count ?? 0,
                Duration = stopwatch.Elapsed
            });
            return completions;
        }

        /// <summary>
        ///     Registers an event handler that searches newly loaded packages
        ///     for extensions to this engine (in particular, for result encoders).
        /// </summary>
        private void RegisterPackageLoadedEvent(IServiceProvider services, ILogger logger, IReferences references)
        {
            var knownAssemblies = references
                .Assemblies
                .Select(asm => asm.Assembly.GetName())
                .ToImmutableHashSet()
                // Except assemblies known at compile-time as well.
                .Add(typeof(StateVectorToHtmlResultEncoder).Assembly.GetName())
                .Add(typeof(AzureClientErrorToHtmlEncoder).Assembly.GetName());
            foreach (var knownAssembly in knownAssemblies) logger.LogDebug("Loaded known assembly {Name}", knownAssembly.FullName);

            // Register new display encoders when packages load.
            references.PackageLoaded += (sender, args) =>
            {
                logger.LogDebug("Scanning for display encoders and magic symbols after loading {Package}.", args.PackageId);
                foreach (var assembly in references.Assemblies
                                                   .Select(asm => asm.Assembly)
                                                   .Where(asm => !knownAssemblies.Contains(asm.GetName()))
                                                   .Where(asm => !MagicSymbolResolver.MundaneAssemblies.Contains(asm.GetName().Name))
                )
                {
                    // Look for display encoders in the new assembly.
                    logger.LogDebug("Found new assembly {Name}, looking for display encoders and magic symbols.", assembly.FullName);
                    // Use the magic resolver to find magic symbols in the new assembly;
                    // it will cache the results for the next magic resolution.
                    (this.MagicResolver as IMagicSymbolResolver)?.FindMagic(new AssemblyInfo(assembly));

                    // If types from an assembly cannot be loaded, log a warning and continue.
                    var relevantTypes = Enumerable.Empty<Type>();
                    try
                    {
                        relevantTypes = assembly
                            .GetTypes()
                            .Where(type =>
                                !type.IsAbstract &&
                                !type.IsInterface &&
                                typeof(IResultEncoder).IsAssignableFrom(type)
                            );
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Encountered exception loading types from {AssemblyName}.",
                            assembly.FullName
                        );
                        continue;
                    }

                    foreach (var type in relevantTypes)
                    {
                        logger.LogDebug(
                            "Found display encoder {TypeName} in {AssemblyName}; registering.",
                            type.FullName,
                            assembly.FullName
                        );

                        // Try and instantiate the new result encoder, but if it fails, that is likely
                        // a non-critical failure that should result in a warning.
                        try
                        {
                            switch (ActivatorUtilities.CreateInstance(services, type))
                            {
                                case IResultEncoder encoder:
                                    RegisterDisplayEncoder(encoder);
                                    break;

                                case {} other:
                                    logger.LogWarning("Expected object of type IResultEncoder but got {Type}.", other.GetType());
                                    break;

                                default:
                                    logger.LogWarning("Expected object of type IResultEncoder but got null.");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(
                                ex,
                                "Encountered exception loading result encoder {TypeName} from {AssemblyName}.",
                                type.FullName, assembly.FullName
                            );
                        }
                    }
                    knownAssemblies = knownAssemblies.Add(assembly.GetName());
                }
            };
        }

        /// <inheritdoc />
        public override async Task<ExecutionResult> Execute(string input, IChannel channel, CancellationToken token)
        {
            void ReportTaskStatus(object sender, TaskPerformanceArgs args)
            {
                channel.Display(args);
            }

            void ReportTaskCompletion(object sender, TaskCompleteArgs args)
            {
                channel.Display(args);
            }

            // Make sure that all relevant initializations have completed before executing.
            await this.Initialized;
            if (configurationSource.InternalShowPerf)
            {
                performanceMonitor.OnTaskPerformanceAvailable += ReportTaskStatus;
                performanceMonitor.OnTaskCompleteAvailable += ReportTaskCompletion;
            }

            try
            {
                return await base.Execute(input, channel, token);
            }
            finally
            {
                if (configurationSource.InternalShowPerf)
                {
                    performanceMonitor.OnTaskPerformanceAvailable -= ReportTaskStatus;
                    performanceMonitor.OnTaskCompleteAvailable -= ReportTaskCompletion;
                }
            }
        }

        /// <summary>
        /// This is the method used to execute Jupyter "normal" cells. In this case, a normal
        /// cell is expected to have a Q# snippet, which gets compiled and we return the name of
        /// the operations found. These operations are then available for simulation and estimate.
        /// </summary>
        public override async Task<ExecutionResult> ExecuteMundane(string input, IChannel channel)
        {
            channel = channel.WithNewLines();
            using var perfTask = performanceMonitor.BeginTask("Mundane cell execution", "execute-mundane");

            void ForwardCompilerTask(QsCompiler.Diagnostics.CompilationTaskEventType type, string? parentTaskName, string taskName)
            {
                channel.Display(new ForwardedCompilerPerformanceEvent(
                    type,
                    parentTaskName,
                    taskName,
                    perfTask!.TimeSinceStart
                ));
            }

            if (configurationSource.InternalShowCompilerPerf)
            {
                QsCompiler.Diagnostics.PerformanceTracking.CompilationTaskEvent += ForwardCompilerTask;
            }

            void DisplayFancyDiagnostics(ISnippets snippets, IEnumerable<Diagnostic> diagnostics)
            {
                var defaultPath = new Snippet().FileName;
                var sources = snippets.Items.ToDictionary(
                    s => s.FileName,
                    s => s.Code
                );
                foreach (var m in diagnostics)
                {
                    var source = m.Source is {} path
                        ? sources.TryGetValue(path, out var snippet)
                            ? snippet
                            : path == defaultPath
                                ? input
                                : null
                        : null;
                    channel.Display(new FancyError(source, m));
                }
            }

            return await Task.Run(async () =>
            {
                // Since this method is only called once this.Initialized
                // has completed, we know that Workspace
                // and Snippets are both not-null.
                Debug.Assert(
                    this.Initialized.IsCompleted,
                    "Engine was not initialized before call to ExecuteMundane. " +
                    "This is an internal error; if you observe this message, please file a bug report at https://github.com/microsoft/iqsharp/issues/new."
                );
                perfTask.ReportStatus("Initialized engine.", "init-engine");

                var workspace = this.Workspace!;
                var snippets = this.Snippets!;
                await workspace.Initialization;
                try
                {
                    perfTask.ReportStatus("Initialized workspace.", "init-workspace");
                    var capability = services.GetRequiredService<IAzureClient>().TargetCapability;

                    var code = await snippets.Compile(input, capability, perfTask);
                    perfTask.ReportStatus("Compiled snippets.", "compiled-snippets");

                    if (metadataController.IsPythonUserAgent() || configurationSource.CompilationErrorStyle == CompilationErrorStyle.Basic)
                    {
                        foreach (var m in code.Warnings)
                        {
                            channel.Stdout(m);
                        }
                    }
                    else
                    {
                        DisplayFancyDiagnostics(snippets, code.Diagnostics);
                    }

                    // Gets the names of all the operations found for this snippet
                    var opsNames =
                        code.Elements?
                            .Where(e => e.IsQsCallable)
                            .Select(e => e.ToFullName().WithoutNamespace(IQSharp.Snippets.SNIPPETS_NAMESPACE))
                            .ToArray();

                    return opsNames.ToExecutionResult();
                }
                catch (CompilationErrorsException c)
                {
                    // Check if the user likely tried to execute a magic
                    // command and try to give a more helpful message in that
                    // case.
                    if (input.TrimStart().StartsWith("%") && input.Split("\n").Length == 1)
                    {
                        var attemptedMagic = input.Split(" ", 2)[0];
                        channel.Stderr($"No such magic command {attemptedMagic}.");
                        if (MagicResolver is MagicSymbolResolver iqsResolver)
                        {
                            var similarMagic = iqsResolver
                                .FindAllMagicSymbols()
                                .Select(symbol =>
                                    (symbol.Name, symbol.Name.EditDistanceFrom(attemptedMagic))
                                )
                                .OrderBy(pair => pair.Item2)
                                .Take(3)
                                .Select(symbol => symbol.Name);
                            channel.Stderr($"Possibly similar magic commands:\n{string.Join("\n", similarMagic.Select(m => $"- {m}"))}");
                        }
                        channel.Stderr($"To get a list of all available magic commands, run %lsmagic, or visit {KnownUris.MagicCommandReference}.");
                    }
                    else
                    {
                        if (metadataController.IsPythonUserAgent() || configurationSource.CompilationErrorStyle == CompilationErrorStyle.Basic)
                        {
                            foreach (var m in c.Errors) channel.Stderr(m);
                        }
                        else
                        {
                           DisplayFancyDiagnostics(snippets, c.Diagnostics);
                        }
                    }
                    return ExecuteStatus.Error.ToExecutionResult();
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, "Exception while executing mundane input: {Input}", input);
                    channel.Stderr(e.Message);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
                finally
                {
                    performanceMonitor.Report();
                    if (configurationSource.InternalShowCompilerPerf)
                    {
                        QsCompiler.Diagnostics.PerformanceTracking.CompilationTaskEvent -= ForwardCompilerTask;
                    }
                }
            });
        }
    }
}

