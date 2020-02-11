// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public class ClientInfoHandler : IShellHandler
    {
        public string UserAgent { get; private set; }
        private readonly ILogger<HeartbeatHandler> logger;
        private readonly IMetadataController metadata;
        public ClientInfoHandler(
            ILogger<HeartbeatHandler> logger,
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
