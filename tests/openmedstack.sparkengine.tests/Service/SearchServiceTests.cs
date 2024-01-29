namespace OpenMedStack.SparkEngine.Tests.Service;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Persistence;
using OpenMedStack.SparkEngine.Search;
using OpenMedStack.SparkEngine.Service.FhirServiceExtensions;
using Xunit;
using Task = System.Threading.Tasks.Task;

public class SearchServiceTests
{
    private readonly SearchService _searchService;
    private readonly IndexService _indexService;

    public SearchServiceTests()
    {
        var fhirModel = new FhirModel();
        var fhirIndex = new InMemoryFhirIndex(NullLogger<InMemoryFhirIndex>.Instance);
        var indexer = new ElementIndexer(fhirModel, NullLogger<ElementIndexer>.Instance);
        var localhost = new Localhost(new Uri("http://localhost"));
        _indexService = new IndexService(fhirModel, fhirIndex, indexer);
        _searchService = new SearchService(localhost, fhirModel, fhirIndex, _indexService);
    }

    [Fact]
    public async Task CanFindRequestedResource()
    {
        await _indexService.IndexResource(new Patient { Id = "Patient/1" }, Key.Create("Patient", "1"));
        var searchParams = SearchParams.FromUriParamList([
            Tuple.Create("_id", "Patient/1")
        ]);
        var results = await _searchService.GetSearchResults("Patient",
            searchParams,
            CancellationToken.None);

        Assert.Equal(1, results.MatchCount);
        Assert.Single(results);
    }

    [Fact]
    public async Task CanFindReverseIncludesForRequestedResource()
    {
        await _indexService.IndexResource(new Patient { Id = "Patient/1" }, Key.Create("Patient", "1"));
        await _indexService.IndexResource(
            new Observation { Id = "Observation/1", Subject = new ResourceReference("Patient/1") },
            Key.Create("Observation", "1"));
        var searchParams = SearchParams.FromUriParamList([
            Tuple.Create("_id", "Patient/1"),
            Tuple.Create("_revinclude:iterate", "Observation.subject")
        ]);
        var results = await _searchService.GetSearchResults("Patient",
            searchParams,
            CancellationToken.None);

        Assert.Equal(1, results.MatchCount);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindsOnlyReverseIncludesForRequestedResource()
    {
        await _indexService.IndexResource(new Patient { Id = "Patient/1" }, Key.Create("Patient", "1"));
        await _indexService.IndexResource(
            new Observation { Id = "Observation/1", Subject = new ResourceReference("Patient/1") },
            Key.Create("Observation", "1"));
        await _indexService.IndexResource(
            new Condition { Id = "Condition/1", Subject = new ResourceReference("Patient/1") },
            Key.Create("Condition", "1"));
        var searchParams = SearchParams.FromUriParamList([
            Tuple.Create("_id", "Patient/1"),
            Tuple.Create("_revinclude:iterate", "Condition.subject")
        ]);
        var results = await _searchService.GetSearchResults("Patient",
            searchParams,
            CancellationToken.None);

        Assert.Equal(1, results.MatchCount);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindsAllReverseIncludesForRequestedResource()
    {
        await _indexService.IndexResource(new Patient { Id = "Patient/1" }, Key.Create("Patient", "1"));
        await _indexService.IndexResource(
            new Observation { Id = "Observation/1", Subject = new ResourceReference("Patient/1") },
            Key.Create("Observation", "1"));
        await _indexService.IndexResource(
            new Condition { Id = "Condition/1", Subject = new ResourceReference("Patient/1") },
            Key.Create("Condition", "1"));
        var searchParams = SearchParams.FromUriParamList([
            Tuple.Create("_id", "Patient/1"),
            Tuple.Create("_revinclude:iterate", "Observation.subject"),
            Tuple.Create("_revinclude:iterate", "Condition.subject")
        ]);
        var results = await _searchService.GetSearchResults("Patient",
            searchParams,
            CancellationToken.None);

        Assert.Equal(1, results.MatchCount);
        Assert.Equal(3, results.Count);
    }
}
