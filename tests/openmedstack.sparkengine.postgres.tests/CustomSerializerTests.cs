namespace OpenMedStack.SparkEngine.Postgres.Tests;

using System.Text;
using Hl7.Fhir.Model;
using Newtonsoft.Json;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using OpenMedStack.SparkEngine.Postgres;

public class CustomSerializerTests
{
    private readonly CustomSerializer _serializer;

    public CustomSerializerTests()
    {
        _serializer = new CustomSerializer(new JsonSerializerSettings());
    }

    [Fact]
    public void CanSerializePatient()
    {
        var patient = new Patient
        {
            Active = true,
            Name = { new HumanName
            {
                Family = "Tester",
                Given = new[]{"John"},
                Text = "John Tester",
                Use = HumanName.NameUse.Usual
            } }
        };
        var json = _serializer.ToJson(patient);
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var p = _serializer.FromJson(typeof(Patient), memoryStream);

        Assert.NotNull(p);
    }

    [Fact]
    public void CanSerializeEnvelope()
    {
        var patient = new Patient
        {
            Active = true,
            Name = { new HumanName
            {
                Family = "Tester",
                Given = new[]{"John"},
                Text = "John Tester",
                Use = HumanName.NameUse.Usual
            } }
        };
        var key = Key.Create(patient.TypeName, "abc");
        var envelope = new ResourceInfo
        {
            Method = Bundle.HTTPVerb.GET,
            IsDeleted = false,
            Id = Guid.NewGuid().ToString("N"),
            IsPresent = true,
            ResourceKey = key.ToStorageKey(),
            ResourceType = patient.TypeName,
            State = EntryState.Internal,
            When = DateTimeOffset.UtcNow,
            ResourceId = patient.Id,
            VersionId = patient.VersionId,
            HasResource = true
        };
        var json = _serializer.ToJson(envelope);
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var e = _serializer.FromJson(typeof(ResourceInfo), memoryStream);

        Assert.NotNull(e);
    }
}