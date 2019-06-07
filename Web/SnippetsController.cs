// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Quantum.IQSharp.Common;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Provides the entry points to manage and simulate Snippets.
    /// Provides the same methods to get the list of snippets and to execute them as the
    /// WorkspaceController (thus the inheritance), plus a "/compile" POST method
    /// to add or update snippets.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SnippetsController : AbstractOperationsController
    {
        public SnippetsController(ISnippets snippets) : base()
        {
            this.Snippets = snippets;
        }

        /// <summary>
        /// The list of available Snippets.
        /// </summary>
        public ISnippets Snippets { get; }

        /// <summary>
        /// Overrides this method to return the list of Operations available from the list of Snippets 
        /// (as opposed to the Operations in the Workspace). These operations behave exactly
        /// as the operations in workspace.
        /// </summary>
        public override IEnumerable<OperationInfo> Operations =>
            Snippets.Operations;

        /// <summary>
        /// Default entry point. Returns the list of operations in the Workspace.
        /// </summary>
        [HttpGet]
        public async Task<Response<string[]>> GetMany() =>
            await AsResponse(async (logger) =>
            await IfReady(async () =>
            {
                var names = Operations?.Select(c => c.FullName).ToArray();
                return names;
            }));

        /// <summary>
        /// A method to add or update a Snippet. If successful, this method returns the list of operations
        /// found and those operations then become available for simulation.
        /// </summary>
        [HttpPost("compile")]
        public async Task<Response<string[]>> Compile(string code) =>
            await AsResponse(async (logger) =>
            await IfReady(async () =>
            {
                var result = Snippets.Compile(code);

                // log warnings:
                foreach (var m in result.warnings) { logger(m); }

                // Gets the names of all the operations found for this snippet
                var opsNames =
                    result.Elements?
                        .Where(e => e.IsQsCallable)
                        .Select(e => e.ToFullName().WithoutNamespace(IQSharp.Snippets.SNIPPETS_NAMESPACE))
                        .OrderBy(o => o)
                        .ToArray();

                return opsNames;
            }));

        /// <summary>
        /// Simulatest the execution of the given operation. 
        /// Supports both, GET and POST.
        /// If GET, then the parameters are expected as normal query parameters
        /// If POST, then the parameters are expected as a JSON object in the body.
        /// </summary>
        [HttpGet("{id}/simulate")]
        [HttpPost("{id}/simulate")]
        public async Task<Response<object>> Simulate(string id) =>
            await AsResponse(async (logger) =>
            await IfReady(async () =>
            await Simulate(id, await GetRunArguments(Request), logger)));

        /// <summary>
        /// Returns an estimation of how many resources are needed to run the given operation on a quantum computer.
        /// As with simulate, supports both, GET and POST.
        /// If GET, then the parameters are expected as normal query parameters
        /// If POST, then the parameters are expected as a JSON object in the body.
        /// </summary>
        [HttpGet("{id}/estimate")]
        [HttpPost("{id}/estimate")]
        public async Task<Response<Dictionary<string, double>>> Estimate(string id) =>
            await AsResponse(async (logger) =>
            await IfReady(async () =>
            await Estimate(id, await GetRunArguments(Request), logger)));

        /// <summary>
        /// Overrides CheckIfReady by not checking if the Workspace is actually available.
        /// It should be possible to build and run self-contained snippets.
        /// </summary>
        public override void CheckIfReady() { }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
