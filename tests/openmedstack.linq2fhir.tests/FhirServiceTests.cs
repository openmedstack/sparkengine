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
    public async System.Threading.Tasks.Task CanQueryListContents()
    {
        var handler = new TestMessageHandler();
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), handler);
        var asyncQueryable = client.Query<Patient>().Where(p => p.Name.Any(n => n.Family == "a"));
        _ = await asyncQueryable.GetBundle();

        Assert.Equal("/Patient?name.family=a", handler.RequestedPathAndQuery);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanQueryListContentsByAnyAttribute()
    {
        var handler = new TestMessageHandler();
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), handler);
        var asyncQueryable = client.Query<Patient>().Where(p => p.Name.Any(n => n.MatchAnyAttribute("a")));
        _ = await asyncQueryable.GetBundle();

        Assert.Equal("/Patient?name=a", handler.RequestedPathAndQuery);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanQueryListContentsByAnyAttributeNotMatching()
    {
        var handler = new TestMessageHandler();
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), handler);
        var asyncQueryable = client.Query<Patient>().Where(p => p.Name.Any(n => n.DoNotMatchAnyAttribute("a")));
        _ = await asyncQueryable.GetBundle();

        Assert.Equal("/Patient?name%3Anot=a", handler.RequestedPathAndQuery);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanReverseIncludeBasedOnReferringTypeAttribute()
    {
        var handler = new TestMessageHandler();
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), handler);

        _ = await client.Query<Patient>()
            .Where(p => p.Name != null)
            .ReverseInclude<Patient, Encounter>(
                x => x.ReferringResource<Observation>().Subject,
                IncludeModifier.Iterate,
                CancellationToken.None)
            .GetBundle()
            .ConfigureAwait(false);

        // ReSharper disable StringLiteralTypo
        Assert.Equal("/Patient?_revinclude%3Aiterate=Observation%3Asubject&name%3Amissing=false", handler.RequestedPathAndQuery);
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

    private static IAsyncQueryable<Patient> GetQueryable()
    {
        var client = new FhirClient("http://localhost", FhirClientSettings.CreateDefault(), new TestMessageHandler());

        var asyncQueryable = client.Query<Patient>()
            .Where(e => e.BirthDate == "a")
            .UpdatedSince(DateTimeOffset.UnixEpoch)
            .OrderBy(x => x.BirthDate)
            .Elements(x => new { s = x.BirthDate, x.Active });
        return asyncQueryable;
    }
}