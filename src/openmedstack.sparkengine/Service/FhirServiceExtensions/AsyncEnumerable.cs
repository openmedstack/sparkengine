namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal static class AsyncEnumerable
{
    public static IAsyncEnumerable<T> Empty<T>()
    {
        return new EmptyAsyncEnumerable<T>();

    }

    private class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return new EmptyAsyncEnumerator<T>();
        }
    }

    private class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(false);
        }

        /// <inheritdoc />
        public T Current { get; } = default(T)!;
    }
}