using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("openmedstack.sparkengine.postgres.tests")]

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Marten;
using Newtonsoft.Json;
using Weasel.Core;

internal class ResourceConverter : JsonConverter<Resource>
{
    private readonly FhirJsonSerializer _inner = new();
    private readonly FhirJsonParser _parser = new();

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Resource? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteRawValue(_inner.SerializeToString(value));
        }
    }

    /// <inheritdoc />
    public override Resource? ReadJson(
        JsonReader reader,
        Type objectType,
        Resource? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        return _parser.Parse(reader, objectType) as Resource;
    }
}

internal class CustomSerializer : ISerializer
{
    private readonly JsonSerializerSettings _settings;

    public CustomSerializer()
    {
        _settings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };
        _settings.Converters.Add(new ResourceConverter());
    }

    /// <inheritdoc />
    public string ToJson(object? document)
    {
        return JsonConvert.SerializeObject(document, _settings);
    }

    /// <inheritdoc />
    public T FromJson<T>(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(streamReader);
        return JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), _settings)!;
    }

    /// <inheritdoc />
    public T FromJson<T>(DbDataReader reader, int index)
    {
        using var textReader = reader.GetTextReader(index);
        using var jsonTextReader = new JsonTextReader(textReader);
        return JsonConvert.DeserializeObject<T>(textReader.ReadToEnd(), _settings)!;
    }

    /// <inheritdoc />
    public async ValueTask<T> FromJsonAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(streamReader);
        return JsonConvert.DeserializeObject<T>(await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false), _settings)!;
    }

    /// <inheritdoc />
    public async ValueTask<T> FromJsonAsync<T>(
        DbDataReader reader,
        int index,
        CancellationToken cancellationToken = default)
    {
        using var textReader = reader.GetTextReader(index);
        var json = await textReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json, _settings)!;
    }

    /// <inheritdoc />
    public object FromJson(Type type, Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(streamReader);
        return JsonConvert.DeserializeObject(streamReader.ReadToEnd(), type, _settings)!;
    }

    /// <inheritdoc />
    public object FromJson(Type type, DbDataReader reader, int index)
    {
        using var textReader = reader.GetTextReader(index);
        using var jsonTextReader = new JsonTextReader(textReader);
        return JsonConvert.DeserializeObject(textReader.ReadToEnd(), type, _settings)!;
    }

    /// <inheritdoc />
    public async ValueTask<object> FromJsonAsync(Type type, Stream stream, CancellationToken cancellationToken = new CancellationToken())
    {
        using var streamReader = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(streamReader);
        return JsonConvert.DeserializeObject(await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false), type, _settings)!;
    }

    /// <inheritdoc />
    public async ValueTask<object> FromJsonAsync(
        Type type,
        DbDataReader reader,
        int index,
        CancellationToken cancellationToken = new CancellationToken())
    {
        using var textReader = reader.GetTextReader(index);
        using var jsonTextReader = new JsonTextReader(textReader);
        return JsonConvert.DeserializeObject(await textReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false), type, _settings)!;
    }

    /// <inheritdoc />
    public string ToCleanJson(object? document)
    {
        return ToJson(document);
    }

    /// <inheritdoc />
    public string ToJsonWithTypes(object document)
    {
        return ToJson(document);
    }

    /// <inheritdoc />
    public EnumStorage EnumStorage { get; } = EnumStorage.AsString;

    /// <inheritdoc />
    public Casing Casing { get; } = Casing.CamelCase;

    /// <inheritdoc />
    public ValueCasting ValueCasting { get; } = ValueCasting.Strict;
}