//namespace OpenMedStack.Linq2Fhir.Tests;

//public class ModelFilterTests
//{
//    [Fact]
//    public void CanFilterCollection()
//    {
//        var collection = new[]
//        {
//            new TestType { Number = 1, Subject = "Lucy" }, new TestType { Number = 2, Subject = "Eve" },
//        };
//        const string query = "number=gt1";
//        var result = collection.Filter(query).OfType<TestType>().ToArray();
//        Assert.Single(result);
//    }
//}
