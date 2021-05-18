// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Azure.Quantum.Jobs;
using Azure.Quantum.Jobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureEnvironmentType { Production, Canary, Dogfood, Mock };

    internal class AzureEnvironment
    {
        public static string EnvironmentVariableName => "AZURE_QUANTUM_ENV";
        public AzureEnvironmentType Type { get; private set; }

        private string SubscriptionId { get; set; } = string.Empty;
        private Func<string?, Uri> BaseUriForLocation { get; set; } = (location) => new Uri($"https://{location}.quantum.azure.com/");
        private ILogger? Logger { get; set; } = null;

        private AzureEnvironment(ILogger? logger = null)
        {
            Logger = logger;
        }

        public static AzureEnvironment Create(string subscriptionId, ILogger? logger = null)
        {
            var azureEnvironmentName = System.Environment.GetEnvironmentVariable(EnvironmentVariableName);

            var environment = Enum.TryParse(azureEnvironmentName, true, out AzureEnvironmentType environmentType)
            ? environmentType switch
              {
                  AzureEnvironmentType.Production =>
                      Production(subscriptionId),
                  AzureEnvironmentType.Canary =>
                      Canary(subscriptionId),
                  AzureEnvironmentType.Dogfood =>
                      Dogfood(subscriptionId),
                  AzureEnvironmentType.Mock =>
                      Mock(),
                  _ =>
                      throw new InvalidOperationException("Unexpected EnvironmentType value.")
              }
            : Production(subscriptionId);

            environment.Logger = logger;
            return environment;
        }

        private string GetNormalizedLocation(string location, IChannel channel)
        {
            // Default to "westus" if no location was specified.
            var defaultLocation = "westus";
            if (string.IsNullOrWhiteSpace(location))
            {
                location = defaultLocation;
            }

            // Convert user-provided location into names recognized by Azure resource manager.
            // For example, a customer-provided value of "West US" should be converted to "westus".
            var normalizedLocation = location.ToLowerInvariant().Replace(" ", "");
            if (UriHostNameType.Unknown == Uri.CheckHostName(normalizedLocation))
            {
                // If provided location is invalid, "westus" is used.
                normalizedLocation = defaultLocation;
                channel.Stdout($"Invalid location {location} specified. Falling back to location {normalizedLocation}.");
            }

            return normalizedLocation;
        }

        public IAzureWorkspace? GetAuthenticatedWorkspaceAsync(
            IChannel channel,
            ILogger? logger,
            string resourceGroupName,
            string workspaceName,
            string location)
        {
            location = GetNormalizedLocation(location, channel);

            switch (Type)
            {
                case AzureEnvironmentType.Mock:
                    channel.Stdout("AZURE_QUANTUM_ENV set to Mock. Using mock Azure workspace rather than connecting to a real service.");
                    return new MockAzureWorkspace(workspaceName, location);

                case AzureEnvironmentType.Canary:
                    channel.Stdout($"AZURE_QUANTUM_ENV set to Canary. Connecting to location eastus2euap rather than specified location {location}.");
                    break;

                case AzureEnvironmentType.Dogfood:
                    channel.Stdout($"AZURE_QUANTUM_ENV set to Dogfood. Connecting to test endpoint rather than production service.");
                    break;

                case AzureEnvironmentType.Production:
                    logger?.LogInformation($"AZURE_QUANTUM_ENV not set, or set to Production. Connecting to production service.");
                    break;
            }

            // Construct and return the AzureWorkspace object
            var azureQuantumWorkspace = new Azure.Quantum.Workspace(
                subscriptionId: SubscriptionId,
                resourceGroupName: resourceGroupName,
                workspaceName: workspaceName,
                location: location);


            var azureQuantumClient = new QuantumJobClient(
                azureQuantumWorkspace.SubscriptionId,
                azureQuantumWorkspace.ResourceGroupName,
                azureQuantumWorkspace.WorkspaceName,
                BaseUriForLocation(location));

            return new AzureWorkspace(azureQuantumClient, azureQuantumWorkspace, location);
        }

        private static AzureEnvironment Production(string subscriptionId) =>
            new AzureEnvironment()
            {
                Type = AzureEnvironmentType.Production,
                SubscriptionId = subscriptionId
            };

        private static AzureEnvironment Dogfood(string subscriptionId) =>
            new AzureEnvironment()
            {
                Type = AzureEnvironmentType.Dogfood,
                BaseUriForLocation = (location) => new Uri($"https://{location}.quantum-test.azure.com/"),
            };

        private static AzureEnvironment Canary(string subscriptionId)
        {
            var canary = Production(subscriptionId);
            canary.BaseUriForLocation = (_) => new Uri($"https://eastus2euap.quantum.azure.com/");
            return canary;
        }

        private static AzureEnvironment Mock() =>
            new AzureEnvironment() { Type = AzureEnvironmentType.Mock };
    }
}
