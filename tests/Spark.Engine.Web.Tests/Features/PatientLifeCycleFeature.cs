namespace Spark.Engine.Web.Tests.Features
{
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Hl7.Fhir.Serialization;
    using Xbehave;
    using Xunit;
    using Xunit.Abstractions;

    public class PatientLifeCycleFeature
    {
        private readonly ITestOutputHelper _outputHelper;
        private TestFhirServer _server;
        private FhirClient _client;

        public PatientLifeCycleFeature(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Background]
        public void Background()
        {
            "Given a running server".x(() => _server = new TestFhirServer(_outputHelper, "https://localhost:60001"));

            "and a FHIR client".x(
                () => _client = new FhirClient(
                    "https://localhost:60001/fhir",
                    new FhirClientSettings
                    {
                        CompressRequestBody = true,
                        ParserSettings = ParserSettings.CreateDefault(),
                        PreferCompressedResponses = true,
                        PreferredFormat = ResourceFormat.Json,
                        UseFormatParameter = false,
                        VerifyFhirVersion = false
                    },
                    messageHandler: _server.Server.CreateHandler()));
        }

        [Scenario(DisplayName = "Patient creation, update and deletion lifecycle")]
        public void PatientCreationUpdateDeletion()
        {
            Patient patient = null!;

            "When creating a patient resource".x(
                async () => patient = await _client.CreateAsync(
                    new Patient
                    {
                        Active = true,
                        Name =
                        {
                            new HumanName
                            {
                                Family = "Tester", Given = new[] {"Anne"}, Use = HumanName.NameUse.Usual
                            }
                        }
                    }).ConfigureAwait(false));

            "Then patient has id".x(() => Assert.NotNull(patient.Id));

            "And patient can be updated".x(
                async () =>
                {
                    patient.BirthDateElement = new Date(1970, 1, 1);
                    patient = await _client.UpdateAsync(patient).ConfigureAwait(false);

                    Assert.NotNull(patient.BirthDate);
                });

            "and can be found when searched".x(
                async () =>
                {
                    var p = await _client.SearchByIdAsync<Patient>(patient.Id).ConfigureAwait(false);
                    Assert.NotEmpty(p.GetResources());
                });

            "When patient can be deleted".x(
                async () =>
                {
                    await _client.DeleteAsync(patient).ConfigureAwait(false);
                });

            "Then cannot be found".x(
                async () =>
                {
                    var p = await _client.SearchByIdAsync<Patient>(patient.Id).ConfigureAwait(false);
                    Assert.Empty(p.GetResources());
                });
        }
    }
}
