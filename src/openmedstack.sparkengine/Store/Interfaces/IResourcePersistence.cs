namespace OpenMedStack.SparkEngine.Store.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;

public interface IResourcePersistence
{
    Task<bool> Store(Resource resource, CancellationToken cancellationToken);
    Task<Resource?> Get(IKey key, CancellationToken cancellationToken);
}