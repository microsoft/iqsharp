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
using Microsoft.Azure.Quantum.Client;
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
        private string ClientId { get; set; } = string.Empty;
        private AadAuthorityAudience Audience { get; set; } = AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount;
        private List<string> Scopes { get; set; } = new List<string>();
        private Func<string?, Uri> BaseUriForLocation { get; set; } = (location) => new Uri($"https://{location}.quantum.azure.com/");
        private string? ArmBaseUrl { get; set; } = null;
        private ILogger? Logger { get; set; } = null;

        private Task<string?> Authority =>
            string.IsNullOrEmpty(SubscriptionId) || string.IsNullOrEmpty(ArmBaseUrl)
            ? Task.FromResult<string?>(null)
            : SubscriptionAuthorityCache.TryGetValue(SubscriptionId, out var authority)
              ? Task.FromResult<string?>(authority)
              : GetAuthorityForSubscription(ArmBaseUrl, SubscriptionId, Logger)
                    .ContinueWith<Task<string?>>(
                        async (Task<string> authority) => await authority
                    )
                    .Unwrap();

        
        /// <summary>
        ///     A map from subscription IDs to authorities used to log in to
        ///     the tenant for each subscription. If a subscription ID is
        ///     missing from this cache, it can be looked up using
        ///     <see cref="GetAuthorityForSubscription(string, string, ILogger?)"/>.
        /// </summary>
        private static Dictionary<string, string> SubscriptionAuthorityCache = new Dictionary<string, string>();

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

        public async Task<IAzureWorkspace?> GetAuthenticatedWorkspaceAsync(
            IChannel channel,
            ILogger? logger,
            string resourceGroupName,
            string workspaceName,
            string location,
            bool refreshCredentials,
            CancellationToken? cancellationToken = null)
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

            // Find the token cache folder
            var cacheDirectoryEnvVarName = "AZURE_QUANTUM_TOKEN_CACHE";
            var cacheDirectory = System.Environment.GetEnvironmentVariable(cacheDirectoryEnvVarName);
            if (string.IsNullOrEmpty(cacheDirectory))
            {
                cacheDirectory = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".azure-quantum");
            }

            // Register the token cache for serialization
            var cacheFileName = "iqsharp.bin";
            var storageCreationProperties = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory, ClientId)
                .WithMacKeyChain(
                    serviceName: "Microsoft.Quantum.IQSharp",
                    accountName: "MSALCache")
                .WithLinuxKeyring(
                    schemaName: "com.microsoft.quantum.iqsharp",
                    collection: "default",
                    secretLabel: "Credentials used by Microsoft IQ# kernel",
                    attribute1: new KeyValuePair<string, string>("Version", typeof(AzureClient).Assembly.GetName().Version.ToString()),
                    attribute2: new KeyValuePair<string, string>("ProductGroup", "QDK"))
                .Build();

            MsalCacheHelper? cacheHelper;
            try
            {
                cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
                cacheHelper.VerifyPersistence();
            }
            catch (MsalCachePersistenceException e)
            {
                // Will occur on Linux if encryption is unavailable. Fallback to unencrypted cache on Linux, as documented here:
                // https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/blob/master/docs/keyring_fallback_proposal.md
                var unencryptedCacheFileName = "iqsharp-unencrypted.bin";
                logger?.LogWarning(e,
                    "Encrypted credential cache is unavailable. Cache will be stored in plaintext at {Path}. Error: {Message}",
                    Path.Combine(cacheDirectory, unencryptedCacheFileName),
                    e.Message);

                storageCreationProperties = new StorageCreationPropertiesBuilder(unencryptedCacheFileName, cacheDirectory, ClientId)
                    .WithMacKeyChain(
                        serviceName: "Microsoft.Quantum.IQSharp",
                        accountName: "MSALCache")
                    .WithLinuxUnprotectedFile()
                    .Build();

                cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
                cacheHelper.VerifyPersistence();
            }

            var msalApp = PublicClientApplicationBuilder.Create(ClientId).WithAuthority(await Authority).Build();
            logger?.LogDebug("Using MSAL authority: {Authority}", msalApp.Authority);
            cacheHelper.RegisterCache(msalApp.UserTokenCache);

            // Perform the authentication
            bool shouldShowLoginPrompt = refreshCredentials;
            AuthenticationResult? authenticationResult = null;
            if (!shouldShowLoginPrompt)
            {
                try
                {
                    var accounts = await msalApp.GetAccountsAsync();
                    var authBuilder = msalApp.AcquireTokenSilent(
                        Scopes, accounts.FirstOrDefault()).WithAuthority(msalApp.Authority);
                    authenticationResult = await (cancellationToken == null
                        ? authBuilder.ExecuteAsync()
                        : authBuilder.ExecuteAsync(cancellationToken.Value));
                }
                catch (MsalUiRequiredException)
                {
                    shouldShowLoginPrompt = true;
                }
            }

            if (shouldShowLoginPrompt)
            {
                IUpdatableDisplay? updater = null;
                var authBuilder = msalApp.AcquireTokenWithDeviceCode(
                    Scopes,
                    deviceCodeResult =>
                    {
                        updater = channel.DisplayUpdatable(deviceCodeResult);
                        return Task.FromResult(0);
                    }).WithAuthority(msalApp.Authority);
                authenticationResult = await (cancellationToken == null
                    ? authBuilder.ExecuteAsync()
                    : authBuilder.ExecuteAsync(cancellationToken.Value));
            }

            if (authenticationResult == null)
            {
                return null;
            }

            // Construct and return the AzureWorkspace object
            var credentials = new Rest.TokenCredentials(authenticationResult.AccessToken);
            var azureQuantumClient = new QuantumClient(credentials)
            {
                SubscriptionId = SubscriptionId,
                ResourceGroupName = resourceGroupName,
                WorkspaceName = workspaceName,
                BaseUri = BaseUriForLocation(location),
            };
            var azureQuantumWorkspace = new Azure.Quantum.Workspace(
                azureQuantumClient.SubscriptionId,
                azureQuantumClient.ResourceGroupName,
                azureQuantumClient.WorkspaceName,
                authenticationResult?.AccessToken,
                BaseUriForLocation(location));

            return new AzureWorkspace(azureQuantumClient, azureQuantumWorkspace, location);
        }

        private static AzureEnvironment Production(string subscriptionId) =>
            new AzureEnvironment()
            {
                Type = AzureEnvironmentType.Production,
                ClientId = "84ba0947-6c53-4dd2-9ca9-b3694761521b",      // QDK client ID
                Scopes = new List<string>() { "https://quantum.microsoft.com/Jobs.ReadWrite" },
                SubscriptionId = subscriptionId,
                ArmBaseUrl = "https://management.azure.com/"
            };

        private static AzureEnvironment Dogfood(string subscriptionId) =>
            new AzureEnvironment()
            {
                Type = AzureEnvironmentType.Dogfood,
                ClientId = "46a998aa-43d0-4281-9cbb-5709a507ac36",      // QDK dogfood client ID
                Scopes = new List<string>() { "api://dogfood.azure-quantum/Jobs.ReadWrite" },
                BaseUriForLocation = (location) => new Uri($"https://{location}.quantum-test.azure.com/"),
                SubscriptionId = subscriptionId,
                ArmBaseUrl = "https://api-dogfood.resources.windows-int.net"
            };

        private static AzureEnvironment Canary(string subscriptionId)
        {
            var canary = Production(subscriptionId);
            canary.Type = AzureEnvironmentType.Canary;
            canary.BaseUriForLocation = (_) => new Uri($"https://eastus2euap.quantum.azure.com/");
            return canary;
        }

        private static AzureEnvironment Mock() =>
            new AzureEnvironment() { Type = AzureEnvironmentType.Mock };

        private static async Task<string> GetAuthorityForSubscription(string armBaseUrl, string subscriptionId, ILogger? logger = null)
        {
            // Start by checking our cache.
            if (SubscriptionAuthorityCache.TryGetValue(subscriptionId, out var cachedAuthority))
            {
                logger?.LogInformation(
                    "Found subscription ID {SubscriptionID} in cache, using authority {Authority}.",
                    subscriptionId, cachedAuthority
                );
                return cachedAuthority;
            }

            try
            {
                var requestUrl = $"{armBaseUrl}/subscriptions/{subscriptionId}?api-version=2018-01-01";

                WebResponse? response = null;
                try
                {
                    response = await WebRequest.Create(requestUrl).GetResponseAsync();
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
                        var authority = quotedAuthority[1..^1];
                        logger?.LogInformation(
                            "ARM reported authority for subscription ID {SubscriptionID}: {Authority}.",
                            subscriptionId, authority
                        );
                        SubscriptionAuthorityCache[subscriptionId] = authority;
                        return authority;
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
