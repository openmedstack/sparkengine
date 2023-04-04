namespace OpenMedStack.FhirServer.AcceptanceTests.StepDefinitions
{
    using Autofac;
    using Autofac.MassTransit;
    using Handlers;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Support;
    using Web.Autofac;
    using Web.Testing;

    [Binding]
    public partial class FeatureSteps
    {
        private readonly CancellationTokenSource _tokenSource = new();
        private TestChassis _chassis = null!;
        private IAsyncDisposable _service = null!;
        private Patient _patient = null!;
        private FhirClient _fhirClient = null!;

        [BeforeScenario]
        public void Setup()
        {
            var configuration = CreateConfiguration();
            _chassis = Chassis.From(configuration)
                .DefinedIn(typeof(ResourceCreatedEventHandler).Assembly)
                .AddAutofacModules((c, _) => new TestFhirModule(c))
                .UsingInMemoryMassTransit()
                .BindToUrls(configuration.Urls)
                .UsingTestWebServer(new TestServerStartup(configuration));
        }

        [AfterScenario]
        public async ValueTask Teardown()
        {
            _tokenSource.Cancel();
            await _service.DisposeAsync();
            _chassis.Dispose();
            _tokenSource.Dispose();
        }

        private static FhirServerConfiguration CreateConfiguration()
        {
            return new FhirServerConfiguration
            {ClientId = "test",
                ConnectionString = "",
                Name = typeof(FeatureSteps).Assembly.GetName().Name,
                RetryCount = 5,
                QueueName = "test",
                ServiceBus = new Uri("loopback://localhost"),
                Urls = Array.Empty<string>(),
                TokenService = "https://localhost",
                AccessKey = "",
                AccessSecret = "",
                StorageServiceUrl = new Uri("loopback://localhost"),
                FhirRoot = "https://localhost/fhir",
                CompressStorage = false,
                Bucket = ""
            };
        }

        [Given(@"a running server setup")]
        public void GivenARunningServerSetup()
        {
            _service = _chassis.Start(_tokenSource.Token);
            Assert.NotNull(_service);
        }

        [Given(@"a FHIR client")]
        public void GivenAFHIRClient()
        {
            _fhirClient = new FhirClient(
                new Uri("https://localhost"),
                _chassis.CreateClient(),
                new FhirClientSettings());
        }

        [Given(@"a FHIR resource")]
        public void GivenAFhirResource()
        {
            _patient = new Patient
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = new List<HumanName> { new() { Family = "Doe", Given = new List<string> { "John" } } },
                Address = new List<Address>
                {
                    new()
                    {
                        Line = new List<string> { "Main Street 1" }, City = "New York", PostalCode = "12345"
                    }
                }
            };
        }

        [When(@"the resource is created")]
        public async System.Threading.Tasks.Task WhenTheResourceIsCreated()
        {
            var response = await _fhirClient.CreateAsync(_patient, _tokenSource.Token);

            Assert.NotNull(response);

            _patient = response!;
        }

        [Then(@"the resource is registered as a UMA resource")]
        public void ThenTheResourceIsRegisteredAsAUMAResource()
        {
            throw new PendingStepException();
        }
    }
}
