// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public static class FhirClientExtensions
    {
        public static bool EqualTo(this IKey key, IKey other)
        {
            return key is not null
                   && key.Base == other?.Base
                   && key.ResourceId == other?.ResourceId
                   && key.TypeName == other?.TypeName
                   && key.VersionId == other?.VersionId;
        }

        public static async IAsyncEnumerable<T> GetAll<T>(
            this FhirClient client,
            string url,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where T : Resource
        {
            var allResources = await client.GetAsync(url).ConfigureAwait(false) as Bundle;
            foreach (var resource in allResources.GetResources().OfType<T>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return resource;
            }

            while (allResources != null && allResources.NextLink != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                allResources = await client.ContinueAsync(allResources).ConfigureAwait(false);
                if (allResources == null)
                {
                    yield break;
                }

                foreach (var resource in allResources.GetResources().OfType<T>())
                {
                    yield return resource;
                }
            }

        }
    }
}