/*
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Extensions;
using Fhir.Metrics;
using Hl7.Fhir.Model;
using Interfaces;

public class ResourceStorageService : IResourceStorageService
{
    private readonly IFhirStore _fhirStore;
    private readonly ILocalhost _localhost;
    private readonly ITransfer _transfer;


    public ResourceStorageService(ITransfer transfer, IFhirStore fhirStore, ILocalhost localhost)
    {
        _transfer = transfer;
        _fhirStore = fhirStore;
        _localhost = localhost;
    }

    /// <inheritdoc />
    public Task<bool> Exists(IKey key, CancellationToken cancellationToken) =>
        _fhirStore.Exists(key, cancellationToken);

    public async Task<ResourceInfo?> Get(IKey key, CancellationToken cancellationToken)
    {
        var entry = await _fhirStore.Get(key, cancellationToken).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public async Task<Resource?> Load(IKey key, CancellationToken cancellationToken)
    {
        var resource = await _fhirStore.Load(key, cancellationToken).ConfigureAwait(false);
        if (resource != null)
        {
            var entry = Entry.Create(resource.ExtractKey(), resource);
            entry = _transfer.Externalize(entry);
            return entry.Resource;
        }

        return resource;
    }

    public async Task<Entry> Add(Entry entry, CancellationToken cancellationToken)
    {
        if (entry.State != EntryState.Internal)
        {
            await _transfer.Internalize(entry, cancellationToken).ConfigureAwait(false);
        }

        var result = await _fhirStore.Add(entry, cancellationToken).ConfigureAwait(false);

        result = _transfer.Externalize(result);

        return result;
    }

    public IAsyncEnumerable<ResourceInfo> Get(
        IEnumerable<string> localIdentifiers,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var interactions = _fhirStore.Get(
            localIdentifiers.Select(k => (IKey)Key.ParseOperationPath(k)),
            cancellationToken);
        return interactions
            .Select(
                x => x with { ResourceKey = x.GetKey().WithBase(_localhost.DefaultBase.AbsoluteUri).ToStorageKey() });
    }
}
