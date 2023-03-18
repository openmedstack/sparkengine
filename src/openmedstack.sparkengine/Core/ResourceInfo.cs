namespace OpenMedStack.SparkEngine.Core;

using System;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

public record ResourceInfo
{
    public required string Id { get; init; }

    public required string? VersionId { get; init; }

    public required string? ResourceType { get; init; }

    public required string ResourceKey { get; init; }

    public required string? ResourceId { get; init; }

    public required Bundle.HTTPVerb Method { get; init; }

    // API: HttpVerb should not be in Bundle.
    public DateTimeOffset? When { get; init; }

    public required EntryState State { get; init; }

    public required bool IsPresent { get; init; }

    public required bool IsDeleted { get; init; }

    public required bool HasResource { get; init; }

    public IKey GetKey()
    {
        return Key.ParseOperationPath(ResourceKey);
    }

    public static ResourceInfo FromEntry(Entry result)
    {
        return new ResourceInfo
        {
            HasResource = result.HasResource(),
            IsDeleted = result.IsDeleted(),
            IsPresent = result.IsPresent,
            Method = result.Method,
            State = result.State,
            When = result.When,
            ResourceKey = result.Key.ToStorageKey(),
            ResourceType = result.Resource?.TypeName,
            VersionId = result.Key.VersionId,
            Id = result.Resource?.Id ?? Guid.NewGuid().ToString("N"),
            ResourceId = result.Resource?.Id
        };
    }
}