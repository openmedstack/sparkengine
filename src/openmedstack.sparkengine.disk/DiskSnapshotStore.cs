namespace OpenMedStack.SparkEngine.Disk;

using Core;
using Interfaces;
using Newtonsoft.Json;

public class DiskSnapshotStore : ISnapshotStore
{
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly string _rootPath;

    public DiskSnapshotStore(DiskPersistenceConfiguration configuration, JsonSerializerSettings serializerSettings)
    {
        _serializerSettings = serializerSettings;
        _rootPath = Path.Combine(configuration.RootPath, "snapshot");
        if (configuration.CreateDirectoryIfNotExists)
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    /// <inheritdoc />
    public Task AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
    {
        var fileName = $"{snapshot.Id}.json";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));
        var json = JsonConvert.SerializeObject(snapshot, _serializerSettings);
        return File.WriteAllTextAsync(fullPath, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Snapshot?> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
    {
        var fileName = $"{snapshotId}.json";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));
        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        return JsonConvert.DeserializeObject<Snapshot>(json, _serializerSettings);
    }
}
