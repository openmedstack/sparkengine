namespace Spark.Engine.Web.Tests
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Xunit.Abstractions;

    public class TestFhirServer
    {
        public TestServer Server { get; }
        public TestFhirServer(ITestOutputHelper outputHelper, params string[] urls)
        {
            var startup = new ServerStartup(outputHelper);
            Server = new TestServer(
                new WebHostBuilder().UseUrls(urls)
                    .ConfigureServices(
                        services =>
                        {
                            startup.ConfigureServices(services);
                        })
                    .UseSetting(WebHostDefaults.ApplicationKey, typeof(ServerStartup).Assembly.FullName)
                    .Configure(startup.Configure));
        }
    }
}
