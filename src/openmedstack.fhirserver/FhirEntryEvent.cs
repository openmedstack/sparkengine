namespace OpenMedStack.FhirServer;

using System;
using Events;
using SparkEngine.Core;

public record FhirEntryEvent : BaseEvent
{
    /// <inheritdoc />
    public FhirEntryEvent(string source, DateTimeOffset timeStamp, ResourceInfo resource, string? correlationId = null)
        : base(source, timeStamp, correlationId)
    {
        Resource = resource;
    }

    public ResourceInfo Resource { get; }
}
