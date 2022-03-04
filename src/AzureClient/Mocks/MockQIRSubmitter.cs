// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Runtime.Submitters;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockQIRSubmitter : IQirSubmitter
    {
        public string Target => throw new NotImplementedException();

        private IReadOnlyList<Argument> Arguments = new List<Argument>();

        public MockQIRSubmitter(IReadOnlyList<Argument> arguments)
        {
            this.Arguments = arguments;
        }

        public Task<IQuantumMachineJob> SubmitAsync(Stream qir, string entryPoint, IReadOnlyList<Argument> arguments, SubmissionOptions options)
        {
            var job = new MockCloudJob();
            MockAzureWorkspace.MockJobIds = new string[] { job.Id };



            return Task.FromResult(job as IQuantumMachineJob);
        }

        public string? Validate(Stream qir, string entryPoint, IReadOnlyList<Argument> arguments)
        {
            throw new NotImplementedException();
        }
    }
}