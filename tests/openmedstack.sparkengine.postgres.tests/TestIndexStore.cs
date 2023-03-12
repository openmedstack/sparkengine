namespace OpenMedStack.SparkEngine.Postgres.Tests;

using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Model;
using OpenMedStack.SparkEngine.Store.Interfaces;

internal class TestIndexStore : IIndexStore
{
    /// <inheritdoc />
    public Task Save(IndexValue indexValue)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Delete(Entry entry)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Clean()
    {
        return Task.CompletedTask;
    }
}