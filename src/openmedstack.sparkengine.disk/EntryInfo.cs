namespace OpenMedStack.SparkEngine.Disk;

using Core;
using Hl7.Fhir.Model;

internal record EntryInfo
{
    public required string ResourceId { get; init; }

    public required string VersionId { get; init; }

    public required string ResourceType { get; init; } = null!;

    public required Bundle.HTTPVerb Verb { get; init; }

    public required string ResourcePath { get; init; }

    public required string Key { get; init; }

    public bool Deleted { get; init; }

    public bool Present { get; init; }

    public DateTimeOffset When { get; init; }

    public EntryState State { get; init; }
}