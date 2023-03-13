﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Threading.Tasks;
using Core;
using Interfaces;
using Marten;
using Microsoft.Extensions.Logging;

public class MartenSnapshotStore : ISnapshotStore
{
    private readonly ILogger<MartenSnapshotStore> _logger;
    private readonly Func<IDocumentSession> _sessionFunc;

    public MartenSnapshotStore(Func<IDocumentSession> sessionFunc, ILogger<MartenSnapshotStore> logger)
    {
        _sessionFunc = sessionFunc;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddSnapshot(Snapshot snapshot)
    {
        _logger.LogDebug("Snapshot added");
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        session.Store(snapshot);
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Snapshot?> GetSnapshot(string snapshotId)
    {
        _logger.LogDebug("Returned snapshot " + snapshotId);
        var session = _sessionFunc();
        await using var _ = session.ConfigureAwait(false);
        var snapshot = await session.LoadAsync<Snapshot>(snapshotId).ConfigureAwait(false);

        return snapshot;
    }
}