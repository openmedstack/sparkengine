namespace OpenMedStack.Linq2Fhir;

using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Provider;

/// <summary>
/// Defines the queryable proxy for a FHIR server.
/// </summary>
public class FhirService
{
    private readonly FhirClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirService"/> class.
    /// </summary>
    /// <param name="client"></param>
    public FhirService(FhirClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets a queryable source for FHIR data.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Resource"/> being queried.</typeparam>
    /// <returns>An <see cref="IAsyncQueryable{T}"/> instance.</returns>
    public IAsyncQueryable<T> Query<T>()
        where T : Resource, new()
    {
        return new RestGetQueryable<T>(_client);
    }
}