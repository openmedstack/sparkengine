namespace OpenMedStack.Linq2Fhir.Tests;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

public class FhirServiceTests
{
    [Fact]
    public async System.Threading.Tasks.Task CanQueryResourcesToList()
    {
        var asyncQueryable = GetQueryable();
        var bundle = await asyncQueryable.ToListAsync();

        Assert.NotNull(bundle);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanQueryResourcesToArray()
    {
        var asyncQueryable = GetQueryable();
        var bundle = await asyncQueryable.ToArrayAsync();

        Assert.NotNull(bundle);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanQueryResourcesToBundle()
    {
        var asyncQueryable = GetQueryable();
        var bundle = await asyncQueryable.GetBundle();

        Assert.NotNull(bundle);
    }

    private static IOrderedAsyncQueryable<Encounter> GetQueryable()
    {
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), new TestMessageHandler());
        
        var asyncQueryable = client.Query<Encounter>()
            .Where(e => e.PlannedEndDate == "a")
            .UpdatedSince(DateTimeOffset.UnixEpoch).OrderBy(x => x.PlannedEndDate);
        return asyncQueryable;
    }
}