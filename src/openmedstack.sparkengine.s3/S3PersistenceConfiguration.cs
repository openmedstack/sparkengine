namespace OpenMedStack.SparkEngine.S3;

using System;

/// <summary>
/// Defines the configuration for S3 persistence.
/// </summary>
public record S3PersistenceConfiguration
{
    public S3PersistenceConfiguration(
        string accessKey,
        string secretKey,
        string bucket,
        Uri serviceUrl,
        bool useHttp = false,
        bool usePathStyle = false,
        bool compress = false)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        Bucket = bucket;
        ServiceUrl = serviceUrl;
        UseHttp = useHttp;
        UsePathStyle = usePathStyle;
        Compress = compress;
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
    /// Gets the root bucket for the tenant.
    /// </summary>
    public string Bucket { get; }

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

    /// <summary>
    /// Gets whether the persisted object is compressed.
    /// </summary>
    public bool Compress { get; }
}