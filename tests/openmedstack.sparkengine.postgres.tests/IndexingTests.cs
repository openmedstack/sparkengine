namespace OpenMedStack.SparkEngine.Postgres.Tests;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using OpenMedStack.SparkEngine.Search;
using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;
using Task = System.Threading.Tasks.Task;

public class IndexingTests
{
    [Theory]
    [MemberData(nameof(GetTestResources))]
    public async Task CanIndex(Resource resource)
    {
        var fhirModel = new FhirModel();
        var indexService = new IndexService(
            fhirModel,
            new TestIndexStore(),
            new ElementIndexer(
                fhirModel,
                NullLogger<ElementIndexer>.Instance,
                new ReferenceNormalizationService(new Localhost(new Uri("https://localhost")))));
        var indexValue = await indexService.IndexResource(resource, resource.ExtractKey()).ConfigureAwait(false);
        var indexEntry = indexValue.BuildIndexEntry();

        Assert.NotNull(indexEntry);
    }


    public static IEnumerable<object[]> GetTestResources()
    {
        var deserializer = new FhirJsonPocoDeserializer();
        foreach (var resource in Directory.EnumerateFiles(Path.Combine(".", "Examples"))
                     .Select(file =>
                     {
                         try
                         {
                             return deserializer.DeserializeResource(File.ReadAllText(file));
                         }
                         catch
                         {
                             return null;
                         }
                     })
                     .Where(x => x != null))
        {
            yield return new object[] { resource! };
        }
    }
}
