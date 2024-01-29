using Microsoft.Extensions.Logging;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;

namespace OpenMedStack.SparkEngine.Persistence;

public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly List<Snapshot> _snapshots = new();
    private readonly ILogger<ISnapshotStore> _logger;

    public InMemorySnapshotStore(ILogger<ISnapshotStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Snapshot added");
        _snapshots.Add(snapshot);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<Snapshot?> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Returned snapshot {snapshotId}", snapshotId);
        return Task.FromResult(_snapshots.Find(x => x.Id == snapshotId));
    }
}