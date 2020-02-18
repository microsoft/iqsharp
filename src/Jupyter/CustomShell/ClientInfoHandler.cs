// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    /// <summary>
    ///     Shell handler that registers new information received from the
    ///     client with an appropriate metadata controller. This allows for
    ///     the client to provide metadata not initially available when the
    ///     kernel starts, such as the browser's user agent string.
    /// </summary>
    public class ClientInfoHandler : IShellHandler
    {
        public string UserAgent { get; private set; }
        private readonly ILogger<EchoHandler> logger;
        private readonly IMetadataController metadata;
        public ClientInfoHandler(
            ILogger<EchoHandler> logger,
            IMetadataController metadata
        )
        {
            this.logger = logger;
            this.metadata = metadata;
        }

        public string MessageType => "iqsharp_client_info";

        public void Handle(Message message)
        {
            metadata.UserAgent = (message.Content as UnknownContent).Data["user_agent"] as string;
        }
    }
}
