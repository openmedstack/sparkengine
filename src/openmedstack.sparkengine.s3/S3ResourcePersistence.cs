// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

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
using Extensions;
using Hl7.Fhir.Serialization;
using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Resource = Hl7.Fhir.Model.Resource;

public class S3ResourcePersistence : IResourcePersistence
{
    private readonly AmazonS3Client _client;
    private readonly bool _compress;
    private readonly string _bucket;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;
    private readonly ILogger<S3ResourcePersistence> _logger;
    private readonly IProvideTenant _tenantProvider;

    public S3ResourcePersistence(
        S3PersistenceConfiguration configuration,
        FhirJsonSerializer serializer,
        FhirJsonParser parser,
        ILogger<S3ResourcePersistence> logger,
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
        _serializer = serializer;
        _parser = parser;
        _logger = logger;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<bool> Store(Resource resource, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream();
            var gzip = new GZipStream(stream, CompressionLevel.Optimal, true);
            await using var __ = gzip.ConfigureAwait(false);
            var writer = _compress
                ? new StreamWriter(gzip, leaveOpen: true)
                : new StreamWriter(stream, leaveOpen: true);
            await using var _ = writer.ConfigureAwait(false);
            var jsonWriter = new JsonTextWriter(writer);
            await using var ___ = jsonWriter.ConfigureAwait(false);
            await _serializer.SerializeAsync(resource, jsonWriter).ConfigureAwait(false);
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
            var response = await _client.PutObjectAsync(
                    new PutObjectRequest
                    {
                        AutoResetStreamPosition = false,
                        Key = $"{_tenantProvider.GetTenantName()}/{resource.ExtractKey().ToStorageKey()}",
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
    async Task<Resource?> IResourcePersistence.Get(IKey key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetObjectAsync(
                    new GetObjectRequest { Key = $"{_tenantProvider.GetTenantName()}/{key.ToStorageKey()}", BucketName = _bucket },
                    cancellationToken)
                .ConfigureAwait(false);

            var gzip = new GZipStream(response.ResponseStream, CompressionMode.Decompress, true);
            await using var _ = gzip.ConfigureAwait(false);
            using var streamReader = _compress ? new StreamReader(gzip) : new StreamReader(response.ResponseStream);
            await using var jsonTextReader = new JsonTextReader(streamReader);
            var resource = await _parser.ParseAsync<Resource>(jsonTextReader).ConfigureAwait(false);
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
            return null;
        }
    }
}
