namespace OpenMedStack.SparkEngine.Tests.Core;

using Newtonsoft.Json;
using SparkEngine.Core;
using Xunit;

public class SnapshotTests
{
    [Fact]
    public void CanDeserialize()
    {
        const string json =
            "{\"includes\":[],\"id\":\"Snapshot/c0a3121f758b4e10ab3a84656ecd4e89\",\"type\":7,\"keys\":{\"$type\":\"OpenMedStack.SparkEngine.Core.SearchResults, openmedstack.sparkengine\",\"$values\":[\"Patient/e59f589a586840f399720f16a7925dca/_history/1f1f79cb549c47b6971101dfc3740466\",\"Patient/e59f589a586840f399720f16a7925dca\"]},\"feedSelfLink\":\"https://fhir.reimers.dk/fhir/Patient?name=Roxane&_count=100&_sort=name:desc&_sort=-name:asc\",\"count\":2,\"countParam\":100,\"whenCreated\":\"2023-03-15T09:39:52.6768006+00:00\",\"sortBy\":\"name\",\"reverseIncludes\":[]}";
        var snapshot = JsonConvert.DeserializeObject<Snapshot>(json);

        Assert.NotNull(snapshot);
    }
}
