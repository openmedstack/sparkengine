using DotAuth.Uma;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

internal class TestResourceMap : StaticResourceMap, IResourceMapper
{
    public TestResourceMap(IReadOnlySet<KeyValuePair<string, string>> mappings) : base(mappings)
    {
    }

    public Task MapResource(string resourceId, string resourceSetId, CancellationToken cancellationToken = default)
    {
        MappedResourcesCount++;
        return Task.CompletedTask;
    }

    public int MappedResourcesCount { get; private set; }
}
