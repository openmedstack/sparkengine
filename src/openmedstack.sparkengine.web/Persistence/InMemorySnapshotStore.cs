﻿namespace OpenMedStack.SparkEngine.Web.Persistence;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Store.Interfaces;

public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly List<Snapshot> _snapshots = new();
    private readonly ILogger<ISnapshotStore> _logger;

    public InMemorySnapshotStore(ILogger<ISnapshotStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task AddSnapshot(Snapshot snapshot)
    {
        _logger.LogDebug("Snapshot added");
        _snapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Snapshot?> GetSnapshot(string snapshotId)
    {
        _logger.LogDebug("Returned snapshot {snapshotId}", snapshotId);
        return Task.FromResult(_snapshots.Find(x => x.Id == snapshotId));
    }
}