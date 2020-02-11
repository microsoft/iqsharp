// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public class ClientInfoHandler : ICustomShellHandler
    {
        public string UserAgent { get; private set; }
        private readonly ILogger<HeartbeatHandler> logger;
        public ClientInfoHandler(
            ILogger<HeartbeatHandler> logger
        )
        {
            this.logger = logger;
        }

        public string MessageType => "iqsharp_client_info";

        public void Handle(Message message)
        {
            UserAgent = (message.Content as UnknownContent).Data["user_agent"] as string;
            logger.LogInformation("User agent: {Agent}", UserAgent);
        }
    }
}
