using DotAuth.Uma;

namespace OpenMedStack.FhirServer.AcceptanceTests.Support;

internal class TestResourceMap : StaticResourceMap, IResourceMapper
{
    public TestResourceMap(IReadOnlySet<KeyValuePair<string, string>> mappings) : base(mappings)
    {
    }

    public new Task MapResource(string resourceId, string resourceSetId, CancellationToken cancellationToken = default)
    {
        MappedResourcesCount++;
        base.MapResource(resourceId, resourceSetId, cancellationToken);
        return Task.CompletedTask;
    }

    public int MappedResourcesCount { get; private set; }
}
