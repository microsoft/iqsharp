// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Quantum.IQSharp.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// The Workspace endpoint. Provides an interface to query for the list of operations
    /// in the workspace and to simulate them.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceController : AbstractOperationsController
    {
        // The default Workspace instance.
        public IWorkspace Workspace { get; set; }

        // The list of operations available in the workspace.
        public override IEnumerable<OperationInfo> Operations =>
            Workspace.Assemblies.SelectMany(asm => asm.Operations);

        public WorkspaceController(IWorkspace workspace)
        {
            this.Workspace = workspace;
        }

        /// <summary>
        /// Default entry point. Returns the list of operations in the Workspace.
        /// </summary>
        [HttpGet]
        public async Task<Response<string[]>> GetMany() => 
            await AsResponse(async(logger) => 
            await IfReady(async () =>
                Operations.Select(c => c.FullName).ToArray()
            ));

        /// <summary>
        /// Get one operation.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<Response<OperationInfo>> GetOne(string id) => 
            await AsResponse(async (logger) => 
            await IfReady(async () =>
            {
                OperationInfo op = Find(id);
                return op;
            }));

        /// <summary>
        /// Triggers a Reload of the Workspace.
        /// </summary>
        [HttpGet("reload")]
        public async Task<Response<string[]>> Reload()
        {
            try
            {
                Workspace.Reload();
                if (Workspace.HasErrors) return new Response<string[]>(Status.Error, Workspace.ErrorMessages.ToArray());
                return await GetMany();
            }
            catch (Exception e)
            {
                return new Response<string[]>(Status.Error, new string[] { e.Message });
            }
        }

        /// <summary>
        /// Simulates the execution of the given operation. 
        /// Supports both, GET and POST.
        /// If GET, then the parameters are expected as normal query parameters.
        /// If POST, then the parameters are expected as a JSON object in the body.
        /// </summary>
        [HttpGet("{id}/simulate")]
        [HttpPost("{id}/simulate")]
        public async Task<Response<object>> Simulate(string id) =>
            await AsResponse(async (logger) =>
            await Simulate(id, await GetRunArguments(Request), logger));


        /// <summary>
        /// Returns an estimate of how many resources are needed to run the given operation on a quantum computer.
        /// As with simulate, supports both, GET and POST.
        /// If GET, then the parameters are expected as normal query parameters
        /// If POST, then the parameters are expected as a JSON object in the body.
        /// </summary>
        [HttpGet("{id}/estimate")]
        [HttpPost("{id}/estimate")]
        public async Task<Response<Dictionary<string, double>>> Estimate(string id) =>
            await AsResponse(async (logger) =>
            await Estimate(id, await GetRunArguments(Request), logger));

        /// <summary>
        /// Performs checks to verify if the Controller is ready to execute operations, namely
        /// it checks if the Workspace is avaialble and in a success (no errors) state.
        /// The method throws Exceptions if it finds it is not ready to execute.
        /// </summary>
        protected override void CheckIfReady()
        {
            if (Workspace == null)
            {
                throw new InvalidWorkspaceException($"Workspace is not ready. Try again.");
            }

            Workspace.Initialization.Wait();
            
            if (Workspace.HasErrors)
            {
                throw new InvalidWorkspaceException(Workspace.ErrorMessages.ToArray());
            }
        }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
