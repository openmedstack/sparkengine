// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.S3
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Core;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Store.Interfaces;


    public class S3ResourcePersistence : IResourcePersistence
    {
        private readonly AmazonS3Client _client;
        private readonly string _bucket;
        private readonly FhirJsonSerializer _serializer;
        private readonly FhirJsonParser _parser;
        private readonly ILogger<S3ResourcePersistence> _logger;

        public S3ResourcePersistence(
            S3PersistenceConfiguration configuration,
            string bucket,
            FhirJsonSerializer serializer,
            FhirJsonParser parser,
            ILogger<S3ResourcePersistence> logger)
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
            _serializer = serializer;
            _parser = parser;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> Store(Resource resource, CancellationToken cancellationToken)
        {
            try
            {
                using var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                await using var _ = writer.ConfigureAwait(false);
                using var jsonWriter = new JsonTextWriter(writer);
                await _serializer.SerializeAsync(resource, jsonWriter).ConfigureAwait(false);
                await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                var response = await _client.PutObjectAsync(
                        new PutObjectRequest
                        {
                            AutoResetStreamPosition = false,
                            Key = resource.ExtractKey().ToStorageKey(),
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
        async Task<Resource?> IResourcePersistence.Get(IKey key, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _client.GetObjectAsync(
                        new GetObjectRequest { Key = key.ToStorageKey(), BucketName = _bucket },
                        cancellationToken)
                    .ConfigureAwait(false);

                using var streamReader = new StreamReader(response.ResponseStream);
                using var jsonTextReader = new JsonTextReader(streamReader);
                var resource = await _parser.ParseAsync<Resource>(jsonTextReader).ConfigureAwait(false);
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{error}", ex.Message);
                return null;
            }
        }
    }
}
