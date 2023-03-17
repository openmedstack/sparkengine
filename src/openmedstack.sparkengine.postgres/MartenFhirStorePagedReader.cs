// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Interfaces;
using Marten;
using Marten.Pagination;

public class MartenFhirStorePagedReader : IFhirStorePagedReader
{
    private readonly Func<IDocumentSession> _sessionFunc;

    public MartenFhirStorePagedReader(Func<IDocumentSession> sessionFunc) => _sessionFunc = sessionFunc;

    /// <inheritdoc />
    public async IAsyncEnumerable<Entry> Read(
        FhirStorePageReaderOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageSize = options?.PageSize ?? 100;
        var pageNumber = 0;
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        while (true)
        {
            var data = await session.Query<ResourceInfo>()
                .OrderBy(x => x.Id)
                .ToPagedListAsync(pageNumber, pageSize, cancellationToken)
                .ConfigureAwait(false);

            foreach (var envelope in data)
            {
                var resource = await session.LoadAsync<Resource>(envelope.ResourceKey, cancellationToken).ConfigureAwait(false);
                yield return Entry.Create(
                    envelope.Method,
                    envelope.GetKey(),
                    resource);
            }

            if (data.IsLastPage)
            {
                break;
            }
        }
    }
}
