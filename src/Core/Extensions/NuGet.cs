// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp.Common;
using NuGet.Protocol.Core.Types;

namespace Microsoft.Quantum.IQSharp
{

    public static class NuGetExtensions
    {
        public static async IAsyncEnumerable<IPackageSearchMetadata> SearchPackagesByIdAsync(
            this SourceRepository repository,
            string packageId,
            bool includePrerelease,
            NuGet.Common.ILogger? logger,
            int batchSize = 50,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var searchResource = await repository.GetResourceAsync<PackageSearchResource>();
            await foreach (var metadata in searchResource
                                           .SearchPackagesByIdAsync(packageId, includePrerelease, logger)
                                           .WithCancellation(cancellationToken))
            {
                yield return metadata;
            }
        }

        public static async IAsyncEnumerable<IPackageSearchMetadata> SearchPackagesByIdAsync(
            this PackageSearchResource searchResource,
            string packageId,
            bool includePrerelease,
            NuGet.Common.ILogger? logger,
            int batchSize = 50,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var skip = 0;
            while (true)
            {
                var metadataBatch = (await searchResource.SearchAsync(
                    packageId,
                    filters: new SearchFilter(includePrerelease: includePrerelease),
                    skip: skip,
                    take: batchSize,
                    logger,
                    cancellationToken
                ))
                .ToList();

                foreach (var metadata in metadataBatch)
                {
                    yield return metadata;
                }
                if (metadataBatch.Count == batchSize)
                {
                    // There may be more...
                    skip += batchSize;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

}
