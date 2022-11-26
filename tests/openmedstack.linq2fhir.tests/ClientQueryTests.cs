namespace OpenMedStack.Linq2Fhir.Tests;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;

public class ClientQueryTests
{
    [Theory]
    [InlineData(ResourceFormat.Json)]
    //[InlineData(ResourceFormat.Xml)]
    public async System.Threading.Tasks.Task CanGetPatientWithDifferentFormats(ResourceFormat format)
    {
        var hc = new HttpClient
        {
        };
        var client = new FhirClient(
            "https://spark.incendi.no/fhir",
            //"https://localhost:7266/fhir",
            //"https://fhir.reimers.dk/fhir",
            //"https://localhost:60001/fhir",
            hc,
            new FhirClientSettings
            {
                CompressRequestBody = true,
                ParserSettings = ParserSettings.CreateDefault(),
                PreferCompressedResponses = true,
                PreferredFormat = format,
                UseFormatParameter = false,
                VerifyFhirVersion = false
            },
            new PocoStructureDefinitionSummaryProvider());

        var result = await client.Query<Observation>().Where(p => p.Specimen != null).GetBundle().ConfigureAwait(false);

        var collection = result.GetResources().ToArray();
        
        Assert.NotEmpty(collection);
    }
}