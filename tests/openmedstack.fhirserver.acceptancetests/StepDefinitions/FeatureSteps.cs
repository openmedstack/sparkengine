using System.Text.RegularExpressions;

namespace OpenMedStack.FhirServer.AcceptanceTests.StepDefinitions;

using System.Net.Http.Headers;
using DotAuth.Client;
using DotAuth.Shared;
using DotAuth.Shared.Responses;
using Microsoft.IdentityModel.Logging;
using Xunit.Abstractions;
using Autofac;
using Autofac.MassTransit;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Support;
using Web.Testing;

public sealed class SharedContext
{
    public Func<HttpMessageHandler> CreateHandler { get; set; } = null!;
}

[Binding]
public partial class FeatureSteps
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly CancellationTokenSource _tokenSource = new();
    private TestChassis<FhirServerConfiguration> _chassis = null!;
    private Patient _patient = null!;
    private FhirClient _fhirClient = null!;
    private FhirServerConfiguration _configuration = null!;
    private TestResourceMap _map = null!;

    public FeatureSteps(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [BeforeScenario]
    public void Setup()
    {
        IdentityModelEventSource.ShowPII = true;
        _configuration = CreateConfiguration();
        _map = new TestResourceMap(new HashSet<KeyValuePair<string, string>>
            { KeyValuePair.Create("abc", "123") });
        _chassis = Chassis.From(_configuration)
            .AddAutofacModules((c, _) => new TestFhirModule<FhirServerConfiguration>(c, _map))
            .UsingInMemoryMassTransit()
            .UsingTestWebServer(new TestServerStartup(_configuration, _outputHelper));
    }

    [AfterScenario]
    public async ValueTask Teardown()
    {
        _tokenSource.Cancel();
        await _chassis.DisposeAsync().ConfigureAwait(false);
        _tokenSource.Dispose();
    }

    private static FhirServerConfiguration CreateConfiguration()
    {
        return new FhirServerConfiguration
        {
            ClientId = "test",
            ConnectionString = "",
            TenantPrefix = "test",
            TopicMap = null,
            Name = typeof(UmaFhirController).Assembly.GetName().Name!,
            RetryCount = 5,
            QueueName = "test",
            ServiceBus = new Uri("loopback://localhost"),
            Urls = new[] { "http://localhost" },
            TokenService = "https://identity.reimers.dk",
            AccessKey = "",
            AccessSecret = "",
            StorageServiceUrl = new Uri("loopback://localhost"),
            FhirRoot = "http://localhost/uma",
            CompressStorage = false,
            Bucket = "", Timeout = TimeSpan.FromMinutes(5),
            Environment = "test",
            Scope = "read",
            Secret = "",
            ServiceBusPassword = "",
            ServiceBusUsername = "", Services = new Dictionary<Regex, Uri>(), ClusterHosts = Array.Empty<string>(),
            ValidIssuers = new[] { "https://identity.reimers.dk" }
        };
    }

    [Given(@"a running server setup")]
    public void GivenARunningServerSetup()
    {
        _chassis.Start();
    }

    [Given(@"a FHIR client")]
    public async System.Threading.Tasks.Task GivenAFHIRClient()
    {
        var tokenClient = new TestTokenClient(_configuration);
        var option =
            await tokenClient.GetToken(TokenRequest.FromScopes("write", "create")).ConfigureAwait(false) as
                Option<GrantedTokenResponse>.Result;
        var token = option!.Item;

        var httpClient = _chassis.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(3);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        _fhirClient = new FhirClient(
            new Uri(_configuration.FhirRoot),
            httpClient,
            new FhirClientSettings { VerifyFhirVersion = false });
    }
}
