namespace OpenMedStack.SparkEngine.Postgres;

using System.Threading;
using System.Threading.Tasks;
using Core;
using Hl7.Fhir.Model;
using Store.Interfaces;
using Task = System.Threading.Tasks.Task;

public class NoOpPersistence : IResourcePersistence
{
    private static readonly NoOpPersistence Instance = new();
    private NoOpPersistence() { }

    public static NoOpPersistence Get() => Instance;

    /// <inheritdoc />
    public Task<bool> Store(Resource resource, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<Resource?> Get(IKey key, CancellationToken cancellationToken)
    {
        return Task.FromResult<Resource?>(null);
    }
}