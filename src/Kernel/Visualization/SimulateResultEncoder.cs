// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    public class SimulateResultToHtmlEncoder : IResultEncoder
    {
        private ICompilerService compilerService;
        public SimulateResultToHtmlEncoder(ICompilerService compilerService)
        {
            this.compilerService = compilerService;
        }

        /// <inheritdoc />
        public string MimeType => MimeTypes.Html;

        /// <summary>
        ///     Checks if a displayable object represents a list of magic symbol
        ///     summaries, and if so, returns an encoding into an HTML table.
        /// </summary>
        public EncodedData? Encode(object displayable)
        {
            if (displayable is not SimulateResult result)
            {
                return null;
            }

            return $"<strong>%simulate {result.OperationName}</strong>: <pre>{result.Output}</pre>".ToEncodedData();
        }
    }

    public class SimulateResultToJsonConverter : JsonConverter<SimulateResult>
    {
        public override SimulateResult ReadJson(JsonReader reader, Type objectType, [AllowNull] SimulateResult existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] SimulateResult value, JsonSerializer serializer)
        {
            // Bypass the result wrapper; it's just for nice displays.
            serializer.Serialize(writer, value?.Output);
        }
    }

}
