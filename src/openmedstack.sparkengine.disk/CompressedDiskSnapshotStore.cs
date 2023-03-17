namespace OpenMedStack.SparkEngine.Disk;

using System.IO.Compression;
using System.Text;
using Core;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class CompressedDiskSnapshotStore : ISnapshotStore
{
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly ILogger<DiskSnapshotStore> _logger;
    private readonly string _rootPath;

    public CompressedDiskSnapshotStore(
        DiskPersistenceConfiguration configuration,
        JsonSerializerSettings serializerSettings,
        ILogger<DiskSnapshotStore> logger)
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
        var fileName = $"{snapshot.Id.Replace('/', '_')}.gz";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));

        _logger.LogInformation("Writing snapshot to {path}", fullPath);

        var fileStream = File.OpenWrite(fullPath);
        await using var _ = fileStream.ConfigureAwait(false);
        var gzip = new GZipStream(fileStream, CompressionLevel.Optimal, true);
        await using var __ = gzip.ConfigureAwait(false);
        var writer = new StreamWriter(gzip, Encoding.UTF8, leaveOpen: true);
        await using var ___ = writer.ConfigureAwait(false);
        using var jsonWriter = new JsonTextWriter(writer);
        var serializer = JsonSerializer.Create(_serializerSettings);
        serializer.Serialize(jsonWriter, snapshot, typeof(Snapshot));
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        await gzip.FlushAsync(cancellationToken).ConfigureAwait(false);
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<Snapshot?> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
    {
        var fileName = $"{snapshotId.Replace('/', '_')}.gz";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));

        _logger.LogInformation("Reading snapshot from {path}", fullPath);

        var fileStream = File.OpenRead(fullPath);
        await using var _ = fileStream.ConfigureAwait(false);
        var gzip = new GZipStream(fileStream, CompressionMode.Decompress, true);
        await using var __ = gzip.ConfigureAwait(false);
        using var reader = new StreamReader(gzip, Encoding.UTF8, leaveOpen: true);
        using var jsonReader = new JsonTextReader(reader);
        var serializer = JsonSerializer.Create(_serializerSettings);
        var snapshot = serializer.Deserialize<Snapshot>(jsonReader);
        return snapshot;
    }
}
