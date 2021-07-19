// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.Linq;
using Microsoft.Quantum.Simulation.Core;

namespace Tests.IQSharp
{
    public class Complex : UDTBase<(double, double)>
    {
        public Complex((double, double) data) : base(data) { }
    }

    public class QubitState : UDTBase<(Complex, Complex)>
    {
        public QubitState((Complex, Complex) data) : base(data) { }
    }

    public class MockKernelOptions : IOptions<KernelContext>
    {
        public KernelContext Value => new KernelContext();
    }

    public class MockNugetOptions : IOptions<NugetPackages.Settings>
    {
        public MockNugetOptions(string[] versions)
        {
            this.Value = new NugetPackages.Settings()
            {
                DefaultPackageVersions = versions
            };
        }

        public NugetPackages.Settings Value { get; }
    }



    public class MockShell : IShellServer
    {
        public event Action<Message>? KernelInfoRequest;
        public event Action<Message>? ExecuteRequest;
        public event Action<Message>? ShutdownRequest; 

        internal void Handle(Message message)
        {
            (message.Header.MessageType switch {
                "kernel_info_request" => KernelInfoRequest,
                "execute_request" => ExecuteRequest,
                "shutdown_request" => ShutdownRequest,
                _ => null
            })?.Invoke(message);
        }

        public void SendIoPubMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public void SendShellMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }
    }

    public class MockShellRouter : IShellRouter
    {
        private MockShell shell;
        public MockShellRouter(MockShell shell)
        {
            this.shell = shell;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Handle(Message message)
        {
            shell.Handle(message);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public void RegisterFallback(Func<Message, Task?> fallback)
        {
            throw new NotImplementedException();
        }

        public void RegisterHandler(string messageType, Func<Message, Task?> handler)
        {
        }

        public void RegisterHandlers<TAssembly>()
        {
        }
    }

    public class MockUpdatableDisplay : IUpdatableDisplay
    {
        public void Update(object displayable)
        {
        }
    }

    public class MockChannel : IChannel
    {
        public List<string> errors = new List<string>();
        public List<string> msgs = new List<string>();
        public List<Message> iopubMessages = new List<Message>();

        public void Display(object displayable)
        {
            
        }

        public IUpdatableDisplay DisplayUpdatable(object displayable)
        {
            return new MockUpdatableDisplay();
        }

        public void SendIoPubMessage(Message message) => iopubMessages.Add(message);

        public void Stderr(string message) => errors.Add(message);

        public void Stdout(string message) => msgs.Add(message);
    }

    public class MockOperationResolver : IOperationResolver
    {
        // NB: We ignore the nullability error here since this mock explicitly
        //     violates the nullability contract. This should only ever return
        //     invalid values, since it shouldn't be called — it's only needed
        //     to make other mock services compile.
        public OperationInfo Resolve(string input) => null!;
    }

    public class MockNugetPackages : INugetPackages
    {
        private static readonly AssemblyInfo MockChemistryAssembly = new AssemblyInfo(typeof(Mock.Chemistry.JordanWignerEncodingData).Assembly);

        private static readonly AssemblyInfo MockStandardAssembly = new AssemblyInfo(typeof(Mock.Standard.ApplyToEach<QubitState>).Assembly);

        List<PackageIdentity> _items = new List<PackageIdentity>();

        public IEnumerable<PackageIdentity> Items => _items;

        public IEnumerable<AssemblyInfo> Assemblies
        {
            get
            {
                var packageIds = _items.Select(p => p.Id);
                if (packageIds.Contains("mock.chemistry"))
                {
                    yield return MockChemistryAssembly;
                }
                else if (packageIds.Contains("mock.standard"))
                {
                    yield return MockStandardAssembly;
                }
            }
        }
        
        public IReadOnlyDictionary<string, NuGetVersion> DefaultVersions => new Dictionary<string, NuGetVersion>();

        public Task<PackageIdentity> Add(string package, Action<string>? statusCallback = null)
        {
            if (package == "microsoft.invalid.quantum")
            {
                throw new NuGet.Resolver.NuGetResolverInputException($"Unable to find package 'microsoft.invalid.quantum'");
            }

            var pkg = new PackageIdentity(package, NuGetVersion.Parse("0.0.0"));
            _items.Add(pkg);
            return Task.FromResult(pkg);
        }
    }
}
