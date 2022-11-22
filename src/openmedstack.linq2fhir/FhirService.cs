namespace OpenMedStack.Linq2Fhir;

using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Provider;

public class FhirService
{
    private readonly FhirClient _client;

    public FhirService(FhirClient client)
    {
        _client = client;
    }

    public IAsyncQueryable<T> Query<T>()
        where T : Resource, new()
    {
        return new RestGetQueryable<T>(_client);
    }
}