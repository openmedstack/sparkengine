namespace OpenMedStack.SparkEngine.Web.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Bogus;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.IdentityModel.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

public class FhirClientLoadTests : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestFhirServer _server;

    public FhirClientLoadTests(ITestOutputHelper outputHelper)
    {
        IdentityModelEventSource.ShowPII = true;
        _outputHelper = outputHelper;
        _server = new TestFhirServer("https://localhost:7266");
    }
    
    [Theory]
    [InlineData(ResourceFormat.Json)]
    //[InlineData(ResourceFormat.Xml)]
    public async Task CanPerformInsertRetrieveAsLoad(ResourceFormat format)
    {
        using var hc = new HttpClient(_server.Server.CreateHandler())
        {
            DefaultRequestHeaders =
            {
                {
                    HeaderNames.Authorization,
                    "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjEiLCJ0eXAiOiJKV1QifQ.eyJzY29wZSI6Im9wZW5pZCBwcm9maWxlIHVtYV9wcm90ZWN0aW9uIiwiYXpwIjoiZGF0YXN0dWRpbyIsInJvbGUiOiIiLCJzdWIiOiIyQzdBQUE1QzgyN0I3RjcxRkQwMjBBRTlCRDk2RjQ5ODgyQ0FFRDcxQTg2QTBFRTA4NTQ3RURDNjI0MTQ1MDIwIiwibmJmIjoxNjY4MTg5MTIwLCJleHAiOjE2NjgxOTA5MjAsImlhdCI6MTY2ODE4OTEyMCwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5yZWltZXJzLmRrIiwiYXVkIjoiZGF0YXN0dWRpbyJ9.jgB-TZXAaT3B9-clt-K5lGpR-LWJx5uyHzchWxIdJNAPvXTC2HYzN2hJO-_AtOjKUdJ2MIv8UIEwTLOq3UZ9uvSErEC5b8O35nAK1YB035jcLkbUem3292X1hfaAyCCP5sU3q6-PhgfjAEAepqqT1SKTwXBwoRu3izoUiqyDHnP_EjdBz-0jNUGNbh3lPb7Z6i56ynBsnB_F0vZ6A3i_4DHq6Is23rTEg2cwAHcsSrYRGePiWpJpvhdyvXF0gZ-cOiNf7JFpfVzYAJ5yKPLthyu9EePHIEC0ms60YW7Fiu1Ajumb3QA_h64GPmNscnYEGpbiqFKcAB5plFg3Hmlvww"
                }
            }
        };
        var client = new FhirClient(
            "https://localhost:7266/fhir",
            hc,
            new FhirClientSettings
            {
                ParserSettings = ParserSettings.CreateDefault(),
                PreferCompressedResponses = true,
                PreferredFormat = format,
                UseFormatParameter = false,
                VerifyFhirVersion = false
            });

        var faker = CreateFaker();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        const int count = 1;
        var tasks = Enumerable.Range(0, count).Select(async _ =>
        {
            var patient = faker.Generate();

            var inserted = await client.CreateAsync(patient).ConfigureAwait(false);

            return inserted!.Id;
        });
        var ids = await Task.WhenAll(tasks).ConfigureAwait(false);
        stopwatch.Stop();

        _outputHelper.WriteLine($"Inserted {count} records at {((double)count * 1000 / stopwatch.ElapsedMilliseconds)} per second");
        var elapsed = stopwatch.Elapsed;
        stopwatch.Restart();
        var patientTasks = ids.Select(id => client.ReadAsync<Patient>($"Patient/{id}"));
        _ = await Task.WhenAll(patientTasks).ConfigureAwait(false);
        stopwatch.Stop();

        elapsed += stopwatch.Elapsed;
        
        _outputHelper.WriteLine($"Read {count} records at {((double)count * 1000 / stopwatch.ElapsedMilliseconds)} per second");
        _outputHelper.WriteLine($"Inserting and reading {count} records took {elapsed}.");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(1));
    }

    private static Faker<Patient> CreateFaker()
    {
        return new Faker<Patient>().RuleFor(x => x.Active, true)
            .RuleFor(
                x => x.Name,
                f =>
                {
                    var lastName = f.Name.LastName();
                    var firstName = f.Name.FirstName();
                    return new List<HumanName>
                    {
                        new()
                        {
                            Family = lastName,
                            Given = new[] { firstName },
                            Text = $"{firstName} {lastName}",
                            Use = HumanName.NameUse.Usual,
                            Prefix = new[] { f.Name.Prefix() },
                            Suffix = new[] { f.Name.Suffix() },
                            Period = new Period { StartElement = new FhirDateTime() }
                        }
                    };
                })
            .RuleFor(x => x.BirthDateElement, f => new Date(f.Date.PastDateOnly(75).Year))
            .RuleFor(x => x.Address,
                f => new List<Address> { new()
                {
                    City = f.Address.City(),
                    Country = f.Address.Country(),
                    District = f.Address.County(),
                    PostalCode = f.Address.ZipCode(),
                    Use = Address.AddressUse.Home
                }});
    }

    [Theory]
    [InlineData(ResourceFormat.Json)]
    [InlineData(ResourceFormat.Xml)]
    public async Task CanQuery(ResourceFormat format)
    {
        using var hc = new HttpClient(_server.Server.CreateHandler())
        {
            DefaultRequestHeaders =
            {
                {
                    HeaderNames.Authorization,
                    "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjEiLCJ0eXAiOiJKV1QifQ.eyJzY29wZSI6Im9wZW5pZCBwcm9maWxlIHVtYV9wcm90ZWN0aW9uIiwiYXpwIjoiZGF0YXN0dWRpbyIsInJvbGUiOiIiLCJzdWIiOiIyQzdBQUE1QzgyN0I3RjcxRkQwMjBBRTlCRDk2RjQ5ODgyQ0FFRDcxQTg2QTBFRTA4NTQ3RURDNjI0MTQ1MDIwIiwibmJmIjoxNjY4MTg5MTIwLCJleHAiOjE2NjgxOTA5MjAsImlhdCI6MTY2ODE4OTEyMCwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5yZWltZXJzLmRrIiwiYXVkIjoiZGF0YXN0dWRpbyJ9.jgB-TZXAaT3B9-clt-K5lGpR-LWJx5uyHzchWxIdJNAPvXTC2HYzN2hJO-_AtOjKUdJ2MIv8UIEwTLOq3UZ9uvSErEC5b8O35nAK1YB035jcLkbUem3292X1hfaAyCCP5sU3q6-PhgfjAEAepqqT1SKTwXBwoRu3izoUiqyDHnP_EjdBz-0jNUGNbh3lPb7Z6i56ynBsnB_F0vZ6A3i_4DHq6Is23rTEg2cwAHcsSrYRGePiWpJpvhdyvXF0gZ-cOiNf7JFpfVzYAJ5yKPLthyu9EePHIEC0ms60YW7Fiu1Ajumb3QA_h64GPmNscnYEGpbiqFKcAB5plFg3Hmlvww"
                }
            }
        };
        var client = new FhirClient(
            "https://localhost:7266/fhir",
            hc,
            new FhirClientSettings
            {
                ParserSettings = ParserSettings.CreateDefault(),
                PreferCompressedResponses = true,
                PreferredFormat = format,
                UseFormatParameter = false,
                VerifyFhirVersion = false
            });
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var searchParams = new SearchParams("name", "Roxane") { Sort = { ("name", SortOrder.Descending) } };
        var u = searchParams.ToUriParamList();
        var queryResult = await client.SearchAsync(searchParams, "Patient").ConfigureAwait(false);
        Assert.True(queryResult.Total > 0);

        stopwatch.Stop();

        var elapsed = stopwatch.Elapsed;

        //_outputHelper.WriteLine($"Read {count} records at {((double)count * 1000 / stopwatch.ElapsedMilliseconds)} per second");
        //_outputHelper.WriteLine($"Inserting and reading {count} records took {elapsed}.");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        //Directory.Delete(Path.GetFullPath(Path.Combine(".", "fhir")), true);
    }
}