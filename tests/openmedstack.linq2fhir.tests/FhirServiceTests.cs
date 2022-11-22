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

    private static IOrderedAsyncQueryable<Encounter> GetQueryable()
    {
        var service = new FhirService(
            new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), new TestMessageHandler()));
        var asyncQueryable = service.Query<Encounter>().Where(e => e.PlannedEndDate == "a").OrderBy(x => x.PlannedEndDate);
        return asyncQueryable;
    }
}