using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using OpenMedStack;
using OpenMedStack.Autofac;
using OpenMedStack.Autofac.MassTransit.RabbitMq;
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

var configuration = CreateConfiguration();

FhirServerConfiguration CreateConfiguration()
{
    var authority = Environment.GetEnvironmentVariable("OAUTH__AUTHORITY");
    var fhirRoot = Environment.GetEnvironmentVariable("FHIR__ROOT");
    var umaRoot = Environment.GetEnvironmentVariable("UMA__ROOT");
    var clientId = Environment.GetEnvironmentVariable("OAUTH__CLIENTID");
    var clientSecret = Environment.GetEnvironmentVariable("OAUTH__CLIENTSECRET");
    var accessKey = Environment.GetEnvironmentVariable("STORAGE__ACCESSKEY");
    var accessSecret = Environment.GetEnvironmentVariable("STORAGE__SECRETKEY");
    var storageUrl = Environment.GetEnvironmentVariable("STORAGE__STORAGEURL");
    var storageCompress = Environment.GetEnvironmentVariable("STORAGE__COMPRESS");
    var storageBucket = Environment.GetEnvironmentVariable("STORAGE__BUCKET");
    var connectionString = Environment.GetEnvironmentVariable("DB__CONNECTIONSTRING");
    var serviceUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var serviceBusHost = Environment.GetEnvironmentVariable("BROKER__HOST");
    var serviceBusUser = Environment.GetEnvironmentVariable("BROKER__USERNAME");
    var serviceBusPassword = Environment.GetEnvironmentVariable("BROKER__PASSWORD");
    var serviceBusQueue = Environment.GetEnvironmentVariable("BROKER__QUEUE");

    CheckParameters(
        authority,
        fhirRoot,
        clientId,
        clientSecret,
        accessKey,
        accessSecret,
        storageUrl,
        storageCompress,
        storageBucket,
        connectionString,
        serviceUrls,
        serviceBusHost,
        serviceBusUser,
        serviceBusPassword,
        serviceBusQueue);
    return new FhirServerConfiguration
    {
        TenantPrefix = "fhir",
        Environment = "Production",
        QueueName = serviceBusQueue!,
        ServiceBus = new Uri(serviceBusHost!),
        ServiceBusUsername = serviceBusUser!,
        ServiceBusPassword = serviceBusPassword!,
        ClientId = clientId!,
        Name = typeof(UmaFhirController).Assembly.GetName().Name!,
        RetryCount = 5,
        RetryInterval = TimeSpan.FromSeconds(5),
        Secret = clientSecret!,
        Timeout = TimeSpan.FromMinutes(5),
        TokenService = authority!,
        ValidIssuers = new[] { authority! },
        AccessKey = accessKey!,
        AccessSecret = accessSecret!,
        StorageServiceUrl = new Uri(storageUrl!),
        Bucket = storageBucket!,
        CompressStorage = bool.TryParse(storageCompress, out var compress) && compress,
        FhirRoot = fhirRoot!,
        UmaRoot = umaRoot!,
        ConnectionString = connectionString!,
        Urls = serviceUrls!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        Services = new Dictionary<Regex, Uri>()
    };
}

var chassis = Chassis.From(configuration)
    .AddAutofacModules((c, _) => new FhirModule(c))
    .UsingMassTransitOverRabbitMq()
//    .UsingInMemoryMassTransit()
    .BindToUrls(configuration.Urls)
    .UsingWebServer(c => new ServerStartup(c));
Console.CancelKeyPress += OnCancelKey;

chassis.Start();
try
{
    waitHandle.Wait(tokenSource.Token);
    await chassis.DisposeAsync().ConfigureAwait(false);
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

void OnCancelKey(object? sender, ConsoleCancelEventArgs e)
{
    // ReSharper disable once AccessToDisposedClosure
    tokenSource.Cancel();
}
