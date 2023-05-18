using Xunit.Abstractions;

namespace OpenMedStack.SparkEngine.Web.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

public class TestFhirServer
{
    public TestServer Server { get; }
    public TestFhirServer(ITestOutputHelper outputHelper, params string[] urls)
    {
        var startup = new ServerStartup();
        Server = new TestServer(
            new WebHostBuilder().UseUrls(urls)
                .ConfigureServices(
                    services =>
                    {
                        startup.ConfigureServices(services, outputHelper);
                    })
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(ServerStartup).Assembly.FullName)
                .Configure(startup.Configure));
    }
}
