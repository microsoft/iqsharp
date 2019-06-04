// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Common;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Provides a mechanism to manage nuget packages.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        public PackagesController(IReferences references)
        {
            this.References = references;
        }

        /// <summary>
        /// Packages are managed via the IReferences
        /// </summary>
        public IReferences References { get; }

        /// <summary>
        /// Default entry point. Returns the list of operations in the Workspace.
        /// </summary>
        [HttpGet]
        public async Task<Response<string[]>> List()
        {
            try
            {
                return new Response<string[]>(Status.success, new string[] { }, References.Packages?.ToArray());
            }
            catch (Exception e)
            {
                return new Response<string[]>(Status.error, new string[] { e.Message });
            }
        }

        /// <summary>
        /// Default entry point. Returns the list of operations in the Workspace.
        /// </summary>
        [HttpGet("add/{pkg}")]
        public async Task<Response<string[]>> Add(string pkg)
        {
            try
            {
                await References.AddPackage(pkg);
                return await List();
            }
            catch (Exception e)
            {
                return new Response<string[]>(Status.error, new string[] { e.Message });
            }
        }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
