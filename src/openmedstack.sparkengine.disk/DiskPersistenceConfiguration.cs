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
    public DiskPersistenceConfiguration(string rootPath)
    {
        RootPath = rootPath;
    }

    /// <summary>
    /// Gets the root of the persistence location.
    /// </summary>
    public string RootPath { get; }
}
