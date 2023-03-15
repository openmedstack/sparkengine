namespace OpenMedStack.SparkEngine.Web.Persistence;

using System;
using System.Threading.Tasks;
using Interfaces;
using OpenMedStack.SparkEngine.Core;

public class InMemoryHistoryStore : IHistoryStore
{
    /// <inheritdoc />
    public Task<Snapshot> History(string typename, HistoryParameters parameters) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Snapshot> History(IKey key, HistoryParameters parameters) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Snapshot> History(HistoryParameters parameters) => throw new NotImplementedException();
}