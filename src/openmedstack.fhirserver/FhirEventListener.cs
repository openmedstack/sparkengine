using OpenMedStack.FhirServer.Handlers;

namespace OpenMedStack.FhirServer;

using System;
using System.Threading.Tasks;
using Events;
using OpenMedStack.Events;
using SparkEngine.Core;
using SparkEngine.Interfaces;

internal class FhirEventListener : IServiceListener
{
    private readonly IResourceMapper _mapper;


    public FhirEventListener(IResourceMapper mapper)
    {
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task Inform(Uri location, Entry interaction)
    {
        await _mapper.MapResource(interaction.Resource!.Id, "123456");
        // var info = ResourceInfo.FromEntry(interaction);
        // var evt = new FhirEntryEvent("fhir", DateTimeOffset.UtcNow, info);
        // return _eventPublisher.Publish(evt);
    }
}
