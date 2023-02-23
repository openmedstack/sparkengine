﻿namespace OpenMedStack.SparkEngine.S3;

using System;

public record S3PersistenceConfiguration
{
    public S3PersistenceConfiguration(
        string accessKey,
        string secretKey,
        Uri serviceUrl,
        bool useHttp = false,
        bool usePathStyle = false)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        ServiceUrl = serviceUrl;
        UseHttp = useHttp;
        UsePathStyle = usePathStyle;
    }

    /// <summary>
    /// Gets the S3 account access key
    /// </summary>
    public string AccessKey { get; }

    /// <summary>
    /// Gets the S3 account secret key
    /// </summary>
    public string SecretKey { get; }

    /// <summary>
    /// Gets the S3 service URL.
    /// </summary>
    public Uri ServiceUrl { get; }

    /// <summary>
    /// Gets whether to use HTTP
    /// </summary>
    public bool UseHttp { get; }

    /// <summary>
    /// Gets whether to use path style bucket naming.
    /// </summary>
    public bool UsePathStyle { get; }
}