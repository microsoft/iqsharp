// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Runtime.Submitters;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockQIRSubmitter : IQirSubmitter
    {
        public string Target => throw new NotImplementedException();

        private IReadOnlyList<Argument> ExpectedArguments = new List<Argument>();

        public MockQIRSubmitter(IReadOnlyList<Argument> expectedArguments)
        {
            this.ExpectedArguments = expectedArguments;
        }

        private bool IsArgumentValueEqual(ArgumentValue fst, ArgumentValue snd) =>
            (fst, snd) switch
            {
                (_, _) when (fst.Type != snd.Type) => false,
                (ArgumentValue.Bool { Value: var fstVal }, ArgumentValue.Bool { Value: var sndVal }) =>
                    fstVal == sndVal,
                (ArgumentValue.Double { Value: var fstVal }, ArgumentValue.Double { Value: var sndVal }) =>
                    fstVal == sndVal,
                (ArgumentValue.Int { Value: var fstVal }, ArgumentValue.Int { Value: var sndVal }) =>
                    fstVal == sndVal,
                (ArgumentValue.String { Value: var fstVal }, ArgumentValue.String { Value: var sndVal }) =>
                    fstVal == sndVal,
                (ArgumentValue.Pauli { Value: var fstVal }, ArgumentValue.Pauli { Value: var sndVal }) =>
                    fstVal == sndVal,
                (ArgumentValue.Result { Value: var fstVal }, ArgumentValue.Result { Value: var sndVal }) =>
                    fstVal == sndVal,
                _ => false
            };

        private bool IsEqualToExpected(IReadOnlyList<Argument> arguments)
        {
            if (this.ExpectedArguments.Count != arguments.Count)
            {
                return false;
            }

            return this.ExpectedArguments
                .Zip(arguments, (fst, snd) => (fst, snd))
                .All(tup => tup.fst.Name == tup.snd.Name && IsArgumentValueEqual(tup.fst.Value, tup.snd.Value));
        }

        public Task<IQuantumMachineJob> SubmitAsync(Stream qir, string entryPoint, IReadOnlyList<Argument> arguments, SubmissionOptions options)
        {
            var job = new MockCloudJob();
            MockAzureWorkspace.MockJobIds = new string[] { job.Id };

            if (!IsEqualToExpected(arguments))
            {
                throw new ArgumentException("The arguments passed to the SubmitAsync did not match the expected arguments to the Mock QIR submitter.");
            }

            return Task.FromResult(job as IQuantumMachineJob);
        }

        public string? Validate(Stream qir, string entryPoint, IReadOnlyList<Argument> arguments)
        {
            throw new NotImplementedException();
        }
    }
}