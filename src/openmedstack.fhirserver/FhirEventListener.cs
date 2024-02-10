namespace OpenMedStack.FhirServer;

using System;
using System.Threading.Tasks;
using Events;
using SparkEngine.Core;
using SparkEngine.Interfaces;

internal class FhirEventListener(IPublishEvents eventPublisher, IProvideApplicationName applicationNameProvider)
    : IServiceListener
{
    /// <inheritdoc />
    public Task Inform(Uri location, Entry interaction)
    {
        var info = ResourceInfo.FromEntry(interaction);
        var evt = new FhirEntryEvent(applicationNameProvider.ApplicationName, DateTimeOffset.UtcNow, info);
        return eventPublisher.Publish(evt);
    }
}
