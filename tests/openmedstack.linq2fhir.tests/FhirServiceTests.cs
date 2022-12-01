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

    [Fact]
    public async System.Threading.Tasks.Task CanReverseIncludeBasedOnReferringTypeAttribute()
    {
        var handler = new TestMessageHandler();
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), handler);

        _ = await client.Query<Patient>()
            .ReverseInclude<Patient, Encounter>(
                x => x.ReferringResource<Encounter>().Subject,
                IncludeModifier.Iterate,
                CancellationToken.None)
            .GetBundle()
            .ConfigureAwait(false);
        
        // ReSharper disable StringLiteralTypo
        Assert.Equal("/Patient?_revinclude%3Aiterate=Encounter%3Asubject", handler.RequestedPathAndQuery);
        // ReSharper restore StringLiteralTypo
    }

    [Fact]
    public async System.Threading.Tasks.Task CanIncludeReferencingAttribute()
    {
        var handler = new TestMessageHandler();
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), handler);
        _ = await client.Query<Patient>()
            .Include(x => x.Link)
            .GetBundle().ConfigureAwait(false);

        // ReSharper disable once StringLiteralTypo
        Assert.Equal("/Patient?_include=Patient%3Alink", handler.RequestedPathAndQuery);
    }

    private static IAsyncQueryable<Encounter> GetQueryable()
    {
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), new TestMessageHandler());

        var asyncQueryable = client.Query<Encounter>()
            .Where(e => e.PlannedEndDate == "a")
            .UpdatedSince(DateTimeOffset.UnixEpoch)
            .OrderBy(x => x.PlannedEndDate)
            .Elements(x => new { s = x.Subject, x.ActualPeriod });
        return asyncQueryable;
    }
}