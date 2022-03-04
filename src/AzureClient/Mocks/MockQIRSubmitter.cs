// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Reflection;
using System.Threading;
using System.Linq;
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

        private IReadOnlyList<Argument> ExpectedArguments = new List<Argument>();

        public MockQIRSubmitter(IReadOnlyList<Argument> expectedArguments)
        {
            this.ExpectedArguments = expectedArguments;
        }

        private bool IsArgumentValueEqual(ArgumentValue fst, ArgumentValue snd)
        {
            if (fst.Type != snd.Type)
            {
                return false;
            }

            if (fst is ArgumentValue.Bool fstBool && snd is ArgumentValue.Bool sndBool)
            {
                return fstBool.Value == sndBool.Value;
            }
            else if (fst is ArgumentValue.Double fstDouble && snd is ArgumentValue.Double sndDouble)
            {
                return fstDouble.Value == sndDouble.Value;
            }
            else if (fst is ArgumentValue.Int fstInt && snd is ArgumentValue.Int sndInt)
            {
                return fstInt.Value == sndInt.Value;
            }
            else if (fst is ArgumentValue.String fstString && snd is ArgumentValue.String sndString)
            {
                return fstString.Value == sndString.Value;
            }
            else if (fst is ArgumentValue.Pauli fstPauli && snd is ArgumentValue.Pauli sndPauli)
            {
                return fstPauli.Value == sndPauli.Value;
            }
            else if (fst is ArgumentValue.Result fstResult && snd is ArgumentValue.Result sndResult)
            {
                return fstResult.Value == sndResult.Value;
            }

            return false;
        }

        private bool IsEqualToExpected(IReadOnlyList<Argument> arguments)
        {
            if (this.ExpectedArguments.Count != arguments.Count)
            {
                return false;
            }

            for (int i = 0; i < this.ExpectedArguments.Count; i++)
            {
                var expected = this.ExpectedArguments[i];
                var given = arguments[i];

                if (expected.Name != given.Name || !IsArgumentValueEqual(expected.Value, given.Value))
                {
                    return false;
                }
            }

            return true;
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