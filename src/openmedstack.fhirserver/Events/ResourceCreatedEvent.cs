namespace OpenMedStack.FhirServer.Events;

using System;
using DotAuth.Shared.Models;
using OpenMedStack.Events;

public record ResourceCreatedEvent : BaseEvent
{
    /// <inheritdoc />
    public ResourceCreatedEvent(
        string source,
        string userToken,
        string resourceId,
        DateTimeOffset timeStamp)
        : base(source,timeStamp)
    {
        UserToken = userToken;
        ResourceId = resourceId;
    }

    public string UserToken { get; }
    public string ResourceId { get; }
}
