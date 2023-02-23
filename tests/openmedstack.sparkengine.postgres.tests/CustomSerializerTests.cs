namespace openmedstack.sparkengine.postgres.tests;

using System.Text;
using Hl7.Fhir.Model;
using OpenMedStack.SparkEngine.Core;
using OpenMedStack.SparkEngine.Extensions;
using OpenMedStack.SparkEngine.Postgres;

public class CustomSerializerTests
{
    private readonly CustomSerializer _serializer;

    public CustomSerializerTests()
    {
        _serializer = new CustomSerializer();
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
        var envelope = new EntryEnvelope
        {
            Method = Bundle.HTTPVerb.GET,
            Deleted = false,
            Id = Guid.NewGuid().ToString("N"),
            IsPresent = true,
            ResourceId = "abc",
            Resource = patient,
            ResourceKey = key.ToStorageKey(),
            ResourceType = patient.TypeName,
            State = EntryState.Internal,
            When = DateTimeOffset.UtcNow
        };
        var json = _serializer.ToJson(envelope);
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var e = _serializer.FromJson(typeof(EntryEnvelope), memoryStream);

        Assert.NotNull(e);
    }
}