namespace OpenMedStack.FhirServer;

using System;
using System.Threading.Tasks;
using Events;
using OpenMedStack.Events;
using SparkEngine.Core;
using SparkEngine.Interfaces;

internal class FhirEventListener : IServiceListener
{
    private readonly IPublishEvents _eventPublisher;

    public FhirEventListener(IPublishEvents eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    /// <inheritdoc />
    public Task Inform(Uri location, Entry interaction)
    {
        var info = ResourceInfo.FromEntry(interaction);
        var evt = new FhirEntryEvent("fhir", DateTimeOffset.UtcNow, info);
        return _eventPublisher.Publish(evt);
    }
}