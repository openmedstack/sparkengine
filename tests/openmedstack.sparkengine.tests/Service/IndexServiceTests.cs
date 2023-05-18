// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Tests.Service;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using Newtonsoft.Json;
using SparkEngine.Core;
using SparkEngine.Search;
using SparkEngine.Search.ValueExpressionTypes;
using SparkEngine.Service.FhirServiceExtensions;
using Xunit;
using static Hl7.Fhir.Model.ModelInfo;
using Task = System.Threading.Tasks.Task;

public class IndexServiceTests
{
    private readonly string _carePlanWithContainedGoal;
    private readonly string _exampleAppointmentJson;
    private readonly string _exampleObservationJson;
    private readonly string _examplePatientJson;
    private readonly IndexService _fullIndexService;
    private readonly IndexService _limitedIndexService;

    public IndexServiceTests()
    {
        var indexStoreMock = new Mock<IIndexStore>();
        _examplePatientJson = File.ReadAllText(
            $".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}patient-example.json");
        _exampleAppointmentJson = File.ReadAllText(
            $".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}appointment-example2doctors.json");
        _carePlanWithContainedGoal = File.ReadAllText(Path.Combine("Examples", "careplan-example-f201-renal_r5.json"));
//            $".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}careplan-example-f201-renal.json");
        _exampleObservationJson = File.ReadAllText(
            $".{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}observation-example-bloodpressure.json");
        var spPatientName = new SearchParamDefinition
        {
            Resource = "Patient",
            Name = "name",
            Description = new Markdown(@"A portion of either family or given name of the patient"),
            Type = SearchParamType.String,
            Path = new[] { "Patient.name" },
            Expression = "Patient.name"
        };
        var spMiddleName = new SearchParamDefinition
        {
            Resource = "Patient",
            Name = "middlename",
            Type = SearchParamType.String,
            Path = new[]
            {
                "Patient.name.extension.where(url='http://hl7.no/fhir/StructureDefinition/no-basis-middlename')"
            },
            Expression =
                "Patient.name.extension.where(url='http://hl7.no/fhir/StructureDefinition/no-basis-middlename')"
        };
        var searchParameters = new List<SearchParamDefinition> { spPatientName, spMiddleName };

        // For this test setup we want a limited available types and search parameters.
        IFhirModel limitedFhirModel = new FhirModel(searchParameters);
        var limitedElementIndexer = new ElementIndexer(limitedFhirModel, new Mock<ILogger<ElementIndexer>>().Object);
        _limitedIndexService = new IndexService(limitedFhirModel, indexStoreMock.Object, limitedElementIndexer);

        // For this test setup we want all available types and search parameters.
        IFhirModel fullFhirModel = new FhirModel();
        var fullElementIndexer = new ElementIndexer(fullFhirModel, new Mock<ILogger<ElementIndexer>>().Object);
        _fullIndexService = new IndexService(fullFhirModel, indexStoreMock.Object, fullElementIndexer);
    }

    [Fact]
    public async Task TestIndexCustomSearchParameter()
    {
        var patient = new Patient();
        var name = new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer");
        name.AddExtension("http://hl7.no/fhir/StructureDefinition/no-basis-middlename", new FhirString("Michel"));
        patient.Name.Add(name);

        IKey patientKey = new Key("http://localhost/", "Patient", "002", "1");
        var result = await _limitedIndexService.IndexResource(patient, patientKey).ConfigureAwait(false);

        var middleName = result.NonInternalValues().Skip(1).First();
        Assert.Equal("middlename", middleName.Name);
        Assert.Single(middleName.Values);
        Assert.IsType<StringValue>(middleName.Values[0]);
        Assert.Equal("Michel", middleName.Values[0].ToString());
    }

    [Fact]
    public async Task TestIndexResourceSimple()
    {
        var patient = new Patient();
        patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

        IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

        var result = await _limitedIndexService.IndexResource(patient, patientKey).ConfigureAwait(false);

        Assert.Equal("root", result.Name);
        Assert.Single(result.NonInternalValues()); //, "Expected 1 non-internal result for searchparameter 'name'");
        var first = result.NonInternalValues().First();
        Assert.Equal("name", first.Name);
        Assert.Equal(2, first.Values.Count);
        Assert.IsType<StringValue>(first.Values[0]);
        Assert.IsType<StringValue>(first.Values[1]);
    }

    [Fact]
    public async Task TestIndexResourcePatientComplete()
    {
        var parser = new FhirJsonParser();
        var patientResource = parser.Parse<Resource>(_examplePatientJson);

        IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

        var result = await _fullIndexService.IndexResource(patientResource, patientKey).ConfigureAwait(false);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SerializeGoal()
    {
        var g =new CarePlan
        {
            Addresses = new List<CodeableReference>{new() { Reference = new ResourceReference("Condition/f204","Roel\u0027s renal insufficiency")}},
            Activity = new List<CarePlan.ActivityComponent>
            {
                new()
                {
                }
            }
        };
        var s = new FhirJsonSerializer();
        var stringWriter = new StringWriter();
        await using var _ = stringWriter.ConfigureAwait(false);
        await using var jsonTextWriter = new JsonTextWriter(stringWriter);
        await s.SerializeAsync(g, jsonTextWriter).ConfigureAwait(false);
        await jsonTextWriter.FlushAsync().ConfigureAwait(false);
        await stringWriter.FlushAsync().ConfigureAwait(false);
        var json = stringWriter.GetStringBuilder().ToString();
    }

    [Fact]
    public async Task TestIndexResourceAppointmentComplete()
    {
        var parser = new FhirJsonParser();
        var appResource = parser.Parse<Resource>(_exampleAppointmentJson);

        IKey appKey = new Key("http://localhost/", "Appointment", "2docs", null);

        var result = await _fullIndexService.IndexResource(appResource, appKey).ConfigureAwait(false);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TestIndexResourceCarePlanWithContainedGoal()
    {
        var parser = new FhirJsonParser();
        var cpResource = parser.Parse<CarePlan>(_carePlanWithContainedGoal);

        IKey cpKey = new Key("http://localhost/", "Careplan", "f002", null);

        var result = await _fullIndexService.IndexResource(cpResource, cpKey).ConfigureAwait(false);

        Assert.NotNull(result);
    }


    [Fact]
    public async Task TestIndexResourceObservation()
    {
        var parser = new FhirJsonParser();
        var obsResource = parser.Parse<Resource>(_exampleObservationJson);

        IKey cpKey = new Key("http://localhost/", "Observation", "blood-pressure", null);

        var result = await _fullIndexService.IndexResource(obsResource, cpKey).ConfigureAwait(false);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TestMultiValueIndexCanIndexFhirDateTime()
    {
        var cd = new Condition { Onset = new FhirDateTime(2015, 6, 15) };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        var result = await _fullIndexService.IndexResource(cd, cdKey).ConfigureAwait(false);

        Assert.NotNull(result);
        var onsetIndex = result.Values.SingleOrDefault(iv => (iv as IndexValue).Name == "onset-date") as IndexValue;
        Assert.NotNull(onsetIndex);
    }

    [Fact]
    public async Task TestMultiValueIndexCanIndexFhirString()
    {
        var onsetInfo = "approximately November 2012";
        var cd = new Condition { Onset = new FhirString(onsetInfo) };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        var result = await _fullIndexService.IndexResource(cd, cdKey).ConfigureAwait(false);

        Assert.NotNull(result);
        var onsetIndex = result.Values.SingleOrDefault(iv => (iv as IndexValue).Name == "onset-info") as IndexValue;
        Assert.NotNull(onsetIndex);
        Assert.True(onsetIndex.Values.Count == 1);
        Assert.True(onsetIndex.Values.First() is StringValue);
        Assert.Equal(onsetInfo, ((StringValue)onsetIndex.Values.First()).Value);
    }

    [Fact]
    public async Task TestMultiValueIndexCanIndexAge()
    {
        decimal onsetAge = 73;
        var cd = new Condition
        {
            Onset = new Age { System = "http://unitsofmeasure.org/", Code = "a", Value = onsetAge }
        };

        IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

        var result = await _fullIndexService.IndexResource(cd, cdKey).ConfigureAwait(false);

        Assert.NotNull(result);
        var onsetIndex = result.Values.Single(iv => (iv as IndexValue).Name == "onset-age") as IndexValue;
        Assert.NotNull(onsetIndex);
        Assert.True(onsetIndex.Values.First() is CompositeValue);
        var composite = onsetIndex.Values.First() as CompositeValue;
        Assert.True(
            composite.Components.Cast<IndexValue>().First(c => c.Name == "value").Values.First() is NumberValue);
        var value =
            composite.Components.Cast<IndexValue>().First(c => c.Name == "value").Values.First() as NumberValue;

        // TODO: Need to convert back to years for this to work, base unit of time is seconds if I am not mistaken.
        //Assert.Equal(onsetAge, value.Value);
    }
}
