namespace OpenMedStack.FhirServer
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddCommandLine(args).AddEnvironmentVariables().Build();
            var builder = new WebHostBuilder().UseConfiguration(config)
                .UseKestrel(
                    options =>
                    {
                        options.AddServerHeader = false;
                        options.Limits.MaxRequestHeadersTotalSize = (int)Math.Pow(2, 16);
                        options.Limits.MaxRequestBodySize = 1024 * 1024;
                        options.Limits.KeepAliveTimeout = TimeSpan.FromHours(1);
                        options.Limits.MaxConcurrentConnections = 10_000;
                    })
                .UseStartup<ServerStartup>();

            var app = builder.Build();

            await app.RunAsync();
        }
    }
}
