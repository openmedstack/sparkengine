namespace OpenMedStack.FhirServer;

using System;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using OpenMedStack.SparkEngine.Interfaces;
using Task = System.Threading.Tasks.Task;

public class GuidGenerator : IGenerator
{
    /// <inheritdoc />
    public Task<string> NextResourceId(Resource resource, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public Task<string> NextVersionId(
        ReadOnlyMemory<char> resourceType,
        ReadOnlyMemory<char> resourceIdentifier,
        ReadOnlyMemory<char> currentVersion,
        CancellationToken cancellationToken) =>
        Task.FromResult(int.TryParse(currentVersion.Span, out var version) ? (version + 1).ToString() : $"{currentVersion}1");
}
