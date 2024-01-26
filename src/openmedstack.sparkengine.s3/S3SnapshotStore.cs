namespace OpenMedStack.SparkEngine.S3;

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Core;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class S3SnapshotStore : ISnapshotStore
{
    private readonly string _bucket;
    private readonly bool _compress;
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly ILogger<S3SnapshotStore> _logger;
    private readonly IProvideTenant _tenantProvider;
    private readonly AmazonS3Client _client;

    public S3SnapshotStore(
        S3PersistenceConfiguration configuration,
        JsonSerializerSettings serializerSettings,
        ILogger<S3SnapshotStore> logger,
        IProvideTenant tenantProvider)
    {
        _compress = configuration.Compress;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(configuration.AccessKey, configuration.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = configuration.ServiceUrl.AbsoluteUri,
                UseHttp = configuration.UseHttp,
                ForcePathStyle = configuration.UsePathStyle
            });
        _bucket = configuration.Bucket;
        _serializerSettings = serializerSettings;
        _logger = logger;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<bool> AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            var serializer = JsonSerializer.Create(_serializerSettings);
            var stream = new MemoryStream();
            await using var __ = stream.ConfigureAwait(false);
            var gzip = new GZipStream(stream, CompressionLevel.Optimal, true);
            await using var ___ = gzip.ConfigureAwait(false);
            var writer = _compress ? new StreamWriter(gzip) : new StreamWriter(stream);
            await using var _ = writer.ConfigureAwait(false);
            await using var jsonWriter = new JsonTextWriter(writer);
            serializer.Serialize(jsonWriter, snapshot, typeof(Snapshot));
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            await gzip.FlushAsync(cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
            var response = await _client.PutObjectAsync(
                    new PutObjectRequest
                    {
                        AutoResetStreamPosition = false,
                        Key = $"{_tenantProvider.GetTenantName()}/{snapshot.Id}",
                        BucketName = _bucket,
                        ContentType = _compress ? "application/gzip" : "application/json",
                        InputStream = stream,
                        DisableDefaultChecksumValidation = true,
                        UseChunkEncoding = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Snapshot?> GetSnapshot(string snapshotId, CancellationToken cancellationToken)
    {
        try
        {
            var serializer = JsonSerializer.Create(_serializerSettings);
            var response = await _client.GetObjectAsync(
                    new GetObjectRequest
                        { Key = $"{_tenantProvider.GetTenantName()}/{snapshotId}", BucketName = _bucket },
                    cancellationToken)
                .ConfigureAwait(false);

            var gzip = new GZipStream(response.ResponseStream, CompressionMode.Decompress, false);
            await using var _ = gzip.ConfigureAwait(false);
            using var streamReader = _compress ? new StreamReader(gzip) : new StreamReader(response.ResponseStream);
            await using var jsonTextReader = new JsonTextReader(streamReader);
            var snapshot = serializer.Deserialize<Snapshot>(jsonTextReader);
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
            return null;
        }
    }
}
