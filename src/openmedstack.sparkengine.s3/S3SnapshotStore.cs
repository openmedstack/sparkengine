namespace OpenMedStack.SparkEngine.S3;

using System;
using System.IO;
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
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly ILogger<S3SnapshotStore> _logger;
    private readonly AmazonS3Client _client;

    public S3SnapshotStore(
        S3PersistenceConfiguration configuration,
        string bucket,
        JsonSerializerSettings serializerSettings,
        ILogger<S3SnapshotStore> logger)
    {
        _client = new AmazonS3Client(
            new BasicAWSCredentials(configuration.AccessKey, configuration.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = configuration.ServiceUrl.AbsoluteUri,
                UseHttp = configuration.UseHttp,
                ForcePathStyle = configuration.UsePathStyle
            });
        _bucket = bucket;
        _serializerSettings = serializerSettings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> AddSnapshot(Snapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            var serializer = JsonSerializer.Create(_serializerSettings);
            await using var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await using var _ = writer.ConfigureAwait(false);
            using var jsonWriter = new JsonTextWriter(writer);
            serializer.Serialize(jsonWriter, snapshot, typeof(Snapshot));
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
            var response = await _client.PutObjectAsync(
                    new PutObjectRequest
                    {
                        AutoResetStreamPosition = false,
                        Key = snapshot.Id,
                        BucketName = _bucket,
                        ContentType = "application/json",
                        InputStream = stream,
                        DisableMD5Stream = true,
                        UseChunkEncoding = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{error}", ex.Message);
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
                    new GetObjectRequest { Key = snapshotId, BucketName = _bucket },
                    cancellationToken)
                .ConfigureAwait(false);

            using var streamReader = new StreamReader(response.ResponseStream);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var snapshot = serializer.Deserialize<Snapshot>(jsonTextReader);
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{error}", ex.Message);
            return null;
        }
    }
}
