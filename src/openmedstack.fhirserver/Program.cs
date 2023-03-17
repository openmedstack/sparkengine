using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using OpenMedStack;
using OpenMedStack.Autofac;
using OpenMedStack.Autofac.MassTransit;
using OpenMedStack.FhirServer;
using OpenMedStack.Web.Autofac;

void CheckParameters(params string?[] values)
{
    if (values.Any(string.IsNullOrWhiteSpace))
    {
        throw new Exception("Needed variable variable missing");
    }
}

using var waitHandle = new ManualResetEventSlim(false);
using var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKey;

void OnCancelKey(object? sender, ConsoleCancelEventArgs e)
{
    // ReSharper disable once AccessToDisposedClosure
    tokenSource.Cancel();
}

var configuration = CreateConfiguration();


FhirServerConfiguration CreateConfiguration()
{
    var authority = Environment.GetEnvironmentVariable("AUTHORITY");
    var fhirRoot = Environment.GetEnvironmentVariable("FHIR__ROOT");
    var clientId = Environment.GetEnvironmentVariable("OAUTH__CLIENTID");
    var clientSecret = Environment.GetEnvironmentVariable("OAUTH__CLIENTSECRET");
    var accessKey = Environment.GetEnvironmentVariable("STORAGE__ACCESSKEY");
    var accessSecret = Environment.GetEnvironmentVariable("STORAGE__SECRETKEY");
    var storageUrl = Environment.GetEnvironmentVariable("STORAGE__STORAGEURL");
    var storageCompress = Environment.GetEnvironmentVariable("STORAGE__COMPRESS");
    var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING");
    var serviceUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var serviceBus = Environment.GetEnvironmentVariable("SERVICEBUS");

    CheckParameters(
        authority,
        fhirRoot,
        clientId,
        clientSecret,
        accessKey,
        accessSecret,
        storageUrl,
        storageCompress,
        connectionString,
        serviceUrls,
        serviceBus);
    return new FhirServerConfiguration
    {
        QueueName = "fhir_server",
        ClientId = clientId!,
        Name = typeof(UmaFhirController).Assembly.GetName().Name!,
        RetryCount = 5,
        RetryInterval = TimeSpan.FromSeconds(5),
        Secret = clientSecret!,
        ServiceBus = new Uri(serviceBus!),
        Timeout = TimeSpan.FromMinutes(5),
        TokenService = authority!,
        ValidIssuers = new[] { authority! },
        AccessKey = accessKey!,
        AccessSecret = accessSecret!,
        StorageServiceUrl = new Uri(storageUrl!),
        CompressStorage = bool.TryParse(storageCompress, out var compress) && compress,
        FhirRoot = fhirRoot!,
        ConnectionString = connectionString!,
        Urls = serviceUrls!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    };
}

var chassis = Chassis.From(configuration)
    .AddAutofacModules((c, _) => new FhirModule(c))
    //.DefinedIn(typeof(ServiceCollectionExtensions).Assembly, typeof(ResourceRegistered).Assembly)
    .UsingInMemoryMassTransit()
    .BindToUrls(configuration.Urls)
    .UsingWebServer(c => new ServerStartup((FhirServerConfiguration)c));
var running = chassis.Start(tokenSource.Token);
try
{
    waitHandle.Wait(tokenSource.Token);
    await running.DisposeAsync().ConfigureAwait(false);
    chassis.Dispose();
}
catch (OperationCanceledException)
{
    // ignore
}
finally
{
    Console.CancelKeyPress -= OnCancelKey;
    Console.WriteLine(@"Shutting down");
}
