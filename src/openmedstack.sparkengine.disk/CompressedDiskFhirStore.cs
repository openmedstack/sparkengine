using Hl7.Fhir.Serialization;

namespace OpenMedStack.SparkEngine.Disk;

using System.IO.Compression;
using Core;
using Hl7.Fhir.Model;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SparkEngine.Extensions;
using Task = System.Threading.Tasks.Task;

public class CompressedDiskFhirStore : AbstractDiskFhirStore
{
    private readonly ILogger<CompressedDiskFhirStore> _logger;

    /// <inheritdoc />
    public CompressedDiskFhirStore(DiskPersistenceConfiguration configuration, ILogger<CompressedDiskFhirStore> logger)
        : base(configuration)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Entry> Add(Entry entry, CancellationToken cancellationToken = default)
    {
        if (entry.IsDelete)
        {
            void MoveEntry(string s)
            {
                try
                {
                    File.Move(Path.Combine(EntryPath, s), Path.Combine(EntryPath, "deleted", s));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not delete entry: {entry}", entry.Key.ToStorageKey());
                }
            }

            var fileName = entry.Key.ToFileName();
            MoveEntry(fileName);
            fileName = entry.Key.WithoutVersion().ToFileName();
            MoveEntry(fileName);

            return entry;
        }

        var json = await Serializer.SerializeToStringAsync(entry.Resource!).ConfigureAwait(false);
        await File.WriteAllTextAsync(
                Path.GetFullPath(Path.Combine(ResourcePath, $"{entry.Key.ToFileName()}.gz")),
                json,
                cancellationToken)
            .ConfigureAwait(false);

        json = JsonConvert.SerializeObject(ResourceInfo.FromEntry(entry));
        await File.WriteAllTextAsync(
            Path.GetFullPath(Path.Combine(EntryPath, $"{entry.Key.ToFileName()}.gz")),
            json,
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.GetFullPath(Path.Combine(EntryPath, $"{entry.Key.WithoutVersion().ToFileName()}.gz")),
            json,
            cancellationToken).ConfigureAwait(false);

        return Entry.Create(entry.Method, entry.Key, entry.Resource);
    }

    /// <inheritdoc />
    public override async Task<ResourceInfo?> Get(IKey key, CancellationToken cancellationToken = default)
    {
        async Task<T?> GetContent<T>(IKey k, CancellationToken token)
        {
            var path = Path.Combine(EntryPath, $"{k.ToFileName()}.gz");

            if (!File.Exists(path))
            {
                return default;
            }

            var fileStream = File.OpenRead(path);
            await using var _ = fileStream.ConfigureAwait(false);
            var gzip = new GZipStream(fileStream, CompressionMode.Decompress, true);
            await using var __ = gzip.ConfigureAwait(false);
            using var streamReader = new StreamReader(path);
            var json = await streamReader.ReadToEndAsync(token).ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(json);
        }

        var entry = await GetContent<ResourceInfo>(key, cancellationToken).ConfigureAwait(false)
                    ?? await GetContent<ResourceInfo>(key.WithoutVersion(), cancellationToken).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public override async Task<Resource?> Load(IKey key, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(EntryPath, $"{key.ToFileName()}.gz");

        if (!File.Exists(path))
        {
            return default;
        }

        var fileStream = File.OpenRead(path);
        await using var _ = fileStream.ConfigureAwait(false);
        var gzip = new GZipStream(fileStream, CompressionMode.Decompress, true);
        await using var __ = gzip.ConfigureAwait(false);
        using var streamReader = new StreamReader(path);

        var json = await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        return Deserializer.DeserializeResource(json);
    }

    /// <inheritdoc />
    public override Task<bool> Exists(IKey key, CancellationToken cancellationToken = default)
    {
        var exists = File.Exists(Path.Combine(EntryPath, $"{key.ToFileName()}.gz"));
        return Task.FromResult(exists);
    }
}
