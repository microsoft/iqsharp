// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    /// <summary>
    ///     Message content to be received when a client asks for an
    ///     experimental feature to be turned on.
    /// </summary>
    public class ExperimentalFeatureContent : MessageContent
    {
        /// <summary>
        ///     The name of the experimental feature to be enabled.
        /// </summary>
        [JsonProperty("feature_name")]
        public string? FeatureName { get; set; }

        /// <summary>
        ///     The names and versions of any optional packages used with the
        ///     requested experimental feature.
        /// </summary>
        [JsonProperty("optional_dependencies")]
        public List<string>? OptionalDependencies { get; set; }
    }

    /// <summary>
    ///     Event type for when a Python client enables an experimental
    ///     feature.
    /// </summary>
    public class ExperimentalFeatureEnabledEvent : Event<ExperimentalFeatureContent>
    {
    }

    /// <summary>
    ///     Shell handler that allows for firing off events when a Python
    ///     client enables an experimental feature via
    ///     <c>qsharp.experimental</c>.
    /// </summary>
    internal class ExperimentalFeaturesShellHandler : IShellHandler
    {
        public string MessageType => "iqsharp_python_enable_experimental";
        private readonly IEventService events;

        public ExperimentalFeaturesShellHandler(IEventService events)
        {
            this.events = events;
        }

        public Task HandleAsync(Message message)
        {
            var content = message.To<ExperimentalFeatureContent>();
            events?.Trigger<ExperimentalFeatureEnabledEvent, ExperimentalFeatureContent>(content);
            return Task.CompletedTask;
        }
    }

}