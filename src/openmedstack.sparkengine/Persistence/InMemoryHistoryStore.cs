using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Interfaces;

namespace OpenMedStack.SparkEngine.Persistence;

public class InMemoryHistoryStore : IHistoryStore
{
    /// <inheritdoc />
    public Task<Snapshot> History(string typename, HistoryParameters parameters) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Snapshot> History(IKey key, HistoryParameters parameters) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Snapshot> History(HistoryParameters parameters) => throw new NotImplementedException();
}