namespace OpenMedStack.SparkEngine.Web.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Interfaces;
using Task = System.Threading.Tasks.Task;

public class GuidGenerator : IGenerator
{
    /// <inheritdoc />
    public Task<string> NextResourceId(Resource resource, CancellationToken cancellationToken) =>
        Task.FromResult(Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public Task<string> NextVersionId(string resourceIdentifier, CancellationToken cancellationToken) =>
        Task.FromResult(Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public Task<string> NextVersionId(
        ReadOnlyMemory<char> resourceType,
        ReadOnlyMemory<char> resourceIdentifier,
        ReadOnlyMemory<char> currentVersion,
        CancellationToken cancellationToken) =>
        Task.FromResult(Guid.NewGuid().ToString("N"));
}
