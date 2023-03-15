namespace OpenMedStack.SparkEngine.Disk;

/// <summary>
/// Defines the disk persistence configuration.
/// </summary>
public record DiskPersistenceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskPersistenceConfiguration"/> class.
    /// </summary>
    /// <param name="rootPath">The root of the persistence location</param>
    /// <param name="createDirectoryIfNotExists">Sets whether to create the persistence directory if it doesn't exist.</param>
    public DiskPersistenceConfiguration(string rootPath, bool createDirectoryIfNotExists)
    {
        RootPath = rootPath;
        CreateDirectoryIfNotExists = createDirectoryIfNotExists;
    }

    /// <summary>
    /// Gets the root of the persistence location.
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// Gets whether to create the persistence directory if it doesn't exist.
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; }
}
