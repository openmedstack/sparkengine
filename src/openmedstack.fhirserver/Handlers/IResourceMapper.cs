namespace OpenMedStack.FhirServer.Handlers;

using System.Threading;
using System.Threading.Tasks;
using DotAuth.Shared.Models;
using DotAuth.Uma;
using OpenMedStack.Commands;

public interface IResourceMapper
{
    Task MapResource(string resourceId, string resourceSetId, CancellationToken cancellationToken = default);
}
