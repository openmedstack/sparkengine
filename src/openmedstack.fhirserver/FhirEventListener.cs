namespace OpenMedStack.FhirServer;

using System;
using System.Threading.Tasks;
using Events;
using SparkEngine.Core;
using SparkEngine.Interfaces;

internal class FhirEventListener : IServiceListener
{
    private readonly IPublishEvents _eventPublisher;
    private readonly IProvideApplicationName _applicationNameProvider;

    public FhirEventListener(IPublishEvents eventPublisher, IProvideApplicationName applicationNameProvider)
    {
        _eventPublisher = eventPublisher;
        _applicationNameProvider = applicationNameProvider;
    }

    /// <inheritdoc />
    public Task Inform(Uri location, Entry interaction)
    {
        var info = ResourceInfo.FromEntry(interaction);
        var evt = new FhirEntryEvent(_applicationNameProvider.ApplicationName, DateTimeOffset.UtcNow, info);
        return _eventPublisher.Publish(evt);
    }
}
