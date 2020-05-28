// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Quantum.Simulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureEnvironmentType { Production, Canary, Dogfood };

    internal class AzureEnvironment
    {
        public string ClientId { get; private set; } = string.Empty;
        public string Authority { get; private set; } = string.Empty;
        public List<string> Scopes { get; private set; } = new List<string>();
        public Uri? BaseUri { get; private set; }

        private AzureEnvironment()
        {
        }

        public static AzureEnvironment Create(string environment, string subscriptionId)
        {
            if (Enum.TryParse(environment, true, out AzureEnvironmentType environmentType))
            {
                switch (environmentType)
                {
                    case AzureEnvironmentType.Production:
                        return Production();
                    case AzureEnvironmentType.Canary:
                        return Canary();
                    case AzureEnvironmentType.Dogfood:
                        return Dogfood(subscriptionId);
                    default:
                        throw new InvalidOperationException("Unexpected EnvironmentType value.");
                }
            }

            return Production();
        }

        private static AzureEnvironment Production() =>
            new AzureEnvironment()
            {
                ClientId = "84ba0947-6c53-4dd2-9ca9-b3694761521b",      // QDK client ID
                Authority = "https://login.microsoftonline.com/common",
                Scopes = new List<string>() { "https://quantum.microsoft.com/Jobs.ReadWrite" },
                BaseUri = new Uri("https://app-jobscheduler-prod.azurewebsites.net/"),
            };

        private static AzureEnvironment Dogfood(string subscriptionId) =>
            new AzureEnvironment()
            {
                ClientId = "46a998aa-43d0-4281-9cbb-5709a507ac36",      // QDK dogfood client ID
                Authority = GetDogfoodAuthority(subscriptionId),
                Scopes = new List<string>() { "api://dogfood.azure-quantum/Jobs.ReadWrite" },
                BaseUri = new Uri("https://app-jobscheduler-test.azurewebsites.net/"),
            };

        private static AzureEnvironment Canary()
        {
            var canary = Production();
            canary.BaseUri = new Uri("https://app-jobs-canarysouthcentralus.azurewebsites.net/");
            return canary;
        }

        private static string GetDogfoodAuthority(string subscriptionId)
        {
            try
            {
                var armBaseUrl = "https://api-dogfood.resources.windows-int.net";
                var requestUrl = $"{armBaseUrl}/subscriptions/{subscriptionId}?api-version=2018-01-01";

                WebResponse? response = null;
                try
                {
                    response = WebRequest.Create(requestUrl).GetResponse();
                }
                catch (WebException webException)
                {
                    response = webException.Response;
                }

                var authHeader = response.Headers["WWW-Authenticate"];
                var headerParts = authHeader.Substring("Bearer ".Length).Split(',');
                foreach (var headerPart in headerParts)
                {
                    var parts = headerPart.Split("=", 2);
                    if (parts[0] == "authorization_uri")
                    {
                        var quotedAuthority = parts[1];
                        return quotedAuthority[1..^1];
                    }
                }

                throw new InvalidOperationException($"Dogfood authority not found in ARM header response for subscription ID {subscriptionId}.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to construct dogfood authority for subscription ID {subscriptionId}.", ex);
            }
        }
    }
}
