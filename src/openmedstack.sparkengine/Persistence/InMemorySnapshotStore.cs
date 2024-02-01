using Microsoft.Extensions.Logging;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;

namespace OpenMedStack.SparkEngine.Persistence;

public class InMemorySnapshotStore(ILogger<InMemorySnapshotStore> logger) : ISnapshotStore
{
    private readonly List<Snapshot> _snapshots = new();
    private readonly ILogger<ISnapshotStore> _logger = logger;

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
        _logger.LogDebug("Returned snapshot {SnapshotId}", snapshotId);
        return Task.FromResult(_snapshots.Find(x => x.Id == snapshotId));
    }
}
