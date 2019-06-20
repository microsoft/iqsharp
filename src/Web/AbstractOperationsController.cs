// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Simulators;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Microsoft.Quantum.IQSharp
{
    public abstract class AbstractOperationsController : ControllerBase
    {
        // The list of operations available in the workspace.
        public abstract IEnumerable<OperationInfo> Operations { get; }

        /// <summary>
        /// Simulates the execution of the given operation using the given arguments
        /// to formulate an input tuple.
        /// </summary>
        public async Task<object> Simulate(string id, IDictionary<string, string> args, Action<string> logger) =>
            await IfReady(async () =>
            {
                using (var qsim = new QuantumSimulator())
                {
                    qsim.DisableLogToConsole();
                    qsim.OnLog += logger;

                    var value = await Find(id).RunAsync(qsim, args);

                    return value;
                }
            });

        /// <summary>
        /// Returns an estimation of how many resources are needed to run the given operation on a quantum computer
        /// with the given arguments.
        /// </summary>
        public async Task<Dictionary<string, double>> Estimate(string id, IDictionary<string, string> args, Action<string> logger) =>
            await IfReady(async () =>
            {
                var qsim = new ResourcesEstimator();
                qsim.DisableLogToConsole();
                qsim.OnLog += logger;

                var value = await Find(id).RunAsync(qsim, args);

                return qsim.AsDictionary();
            });

        /// <summary>
        /// Wrapps the result of calling an asynchronous action into a `Response` object.
        /// If an Exception is caught, it returns an error response with the Exception as the
        /// corresponding error messages.
        /// </summary>
        public virtual async Task<Response<T>> AsResponse<T>(Func<Action<string>, Task<T>> action)
        {
            try
            {
                var messages = new List<string>();
                var result = await action(messages.Add);
                return new Response<T>(Status.success, messages.ToArray(), result);
            }
            catch (InvalidWorkspaceException ws)
            {
                return new Response<T>(Status.error, ws.Errors);
            }
            catch (CompilationErrorsException c)
            {
                return new Response<T>(Status.error, c.Errors);
            }
            catch (Exception e)
            {
                return new Response<T>(Status.error, new string[] { e.Message });
            }
        }

        /// <summary>
        /// Performs checks to verify if the Controller is ready to execute operations, namely
        /// it checks if the Workspace is avaialble and in a success (no errors) state.
        /// The method throws Exceptions if it finds it is not ready to execute.
        /// </summary>
        public abstract void CheckIfReady();

        /// <summary>
        /// Executes the given `action` only if the controller is ready, namely if after calling `CheckIfReady`.
        /// </summary>
        public virtual async Task<T> IfReady<T>(Func<Task<T>> action)
        {
            CheckIfReady();

            return await action();
        }

        /// <summary>
        ///  Finds the given operation within the list of Operations.
        /// </summary>
        internal bool TryFind(string id, out OperationInfo op)
        {
            if (Operations == null)
            {
                throw new ArgumentException($"Workspace is not ready. Try again.");
            }
            else
            {
                op = Operations.FirstOrDefault(o => o.FullName == id);
                return (op != null);
            }
        }

        /// <summary>
        ///  Finds the given operation within the list of Operations. If it can't find it, it sets the Status code to 404.
        /// </summary>
        internal OperationInfo Find(string id)
        {
            var found = TryFind(id, out var op);
            if (!found)
            {
                if (HttpContext?.Response != null) { HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound; }
                throw new ArgumentException($"Invalid operation name: {id}");
            }
            System.Diagnostics.Debug.Assert(op != null);
            return op;
        }

        /// <summary>
        /// Decides where to read the arguments for executing a quantum operation.
        /// First it checks `RunArguments`. If available it uses that (typically for unittests)
        /// Then, it checks if the Request's method is GET, if so, it converts the Query string into a key/value pairs dictionary;
        /// if it is POST, it expects a json object in the body and uses JsonToDict to convert that also into a key/value pairs dictionary
        /// </summary>
        /// <returns></returns>
        internal static async Task<IDictionary<string, string>> GetRunArguments(HttpRequest request)
        {
            if (request == null) return null;
            if (request.Method == "GET") return new Dictionary<string, string>(request.Query.Select(kv => KeyValuePair.Create(kv.Key, kv.Value.ToString())));
            if (request.Method == "POST")
            {
                using (var body = new StreamReader(request.Body))
                {
                    var json = await body.ReadToEndAsync();
                    return TupleConverters.JsonToDict(json);
                }
            }

            return null;
        }
    }
}


#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
