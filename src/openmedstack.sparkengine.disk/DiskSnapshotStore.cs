namespace OpenMedStack.SparkEngine.Disk;

using Core;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class DiskSnapshotStore : ISnapshotStore
{
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly ILogger<DiskSnapshotStore> _logger;
    private readonly string _rootPath;

    public DiskSnapshotStore(DiskPersistenceConfiguration configuration, JsonSerializerSettings serializerSettings, ILogger<DiskSnapshotStore> logger)
    {
        _serializerSettings = serializerSettings;
        _logger = logger;
        _rootPath = Path.Combine(configuration.RootPath, "snapshot");
        if (configuration.CreateDirectoryIfNotExists)
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    /// <inheritdoc />
    public async Task<bool> AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
    {
        var fileName = $"{snapshot.Id.Replace('/', '_')}.json";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));

        _logger.LogInformation("Writing snapshot to {path}", fullPath);

        var json = JsonConvert.SerializeObject(snapshot, _serializerSettings);
        await File.WriteAllTextAsync(fullPath, json, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<Snapshot?> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
    {
        var fileName = $"{snapshotId.Replace('/', '_')}.json";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));

        _logger.LogInformation("Reading snapshot from {path}", fullPath);

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<Snapshot>(json, _serializerSettings);
    }
}