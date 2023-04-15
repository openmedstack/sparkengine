using DotAuth.Uma;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

internal class TestResourceMap: StaticResourceMap, IResourceMapper
{
    private int _mappedResourcesCount;
    public TestResourceMap(IReadOnlySet<KeyValuePair<string, string>> mappings) : base(mappings)
    {
    }

    public Task MapResource(string resourceId, string resourceSetId, CancellationToken cancellationToken = default)
    {
        _mappedResourcesCount++;
        return Task.CompletedTask;
    }

    public int MappedResourcesCount => _mappedResourcesCount;
}
