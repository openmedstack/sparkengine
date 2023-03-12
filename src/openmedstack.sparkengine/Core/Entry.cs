namespace OpenMedStack.SparkEngine.Core;

using System;
using Extensions;
using Hl7.Fhir.Model;

public class Entry
{
    public IKey Key
    {
        get
        {
            return Resource != null && !(Method == Bundle.HTTPVerb.PATCH && Resource is Parameters)
                ? Resource.ExtractKey()
                : _key;
        }
        set
        {
            if (Resource != null)
            {
                value.ApplyTo(Resource);
                _key = value;
            }
            else
            {
                _key = value;
            }
        }
    }

    public Resource? Resource { get; set; }

    public Bundle.HTTPVerb Method { get; private set; }

    // API: HttpVerb should not be in Bundle.
    public DateTimeOffset? When
    {
        get { return Resource is { Meta: { } } ? Resource.Meta.LastUpdated : _when; }
        set
        {
            if (Resource != null)
            {
                Resource.Meta ??= new Meta();
                Resource.Meta.LastUpdated = value?.TruncateToMillis();
            }
            else
            {
                _when = value;
            }
        }
    }

    public EntryState State { get; set; }

    private IKey _key;
    private DateTimeOffset? _when;

    private Entry(Bundle.HTTPVerb method, IKey key, DateTimeOffset? when, Resource? resource)
    {
        _key = key;
        if (resource != null && !(method == Bundle.HTTPVerb.PATCH && resource is Parameters))
        {
            key.ApplyTo(resource);
        }
        else
        {
            Key = key;
        }

        Resource = resource;
        Method = method;
        When = when ?? DateTimeOffset.Now;
        State = EntryState.Undefined;
    }

    protected Entry(IKey key, Resource resource)
    {
        _key = key;
        Key = key;
        Resource = resource;
        State = EntryState.Undefined;
    }

    public static Entry Create(Bundle.HTTPVerb method, Resource resource)
    {
        return new(method, resource.ExtractKey(), DateTimeOffset.UtcNow, resource);
    }

    public static Entry Create(Bundle.HTTPVerb method, IKey key)
    {
        return new Entry(method, key, null, null);
    }

    public static Entry Create(Bundle.HTTPVerb method, IKey key, Resource? resource)
    {
        return new(method, key, DateTimeOffset.UtcNow, resource);
    }

    public static Entry Create(IKey key, Resource resource)
    {
        return new Entry(key, resource);
    }

    public static Entry Create(Bundle.HTTPVerb method, IKey key, DateTimeOffset when)
    {
        return new(method, key, when, null);
    }

    /// <summary>
    ///  Creates a deleted entry
    /// </summary>
    public static Entry Delete(IKey key, DateTimeOffset? when)
    {
        return Create(Bundle.HTTPVerb.DELETE, key, when ?? DateTimeOffset.UtcNow);
    }

    public bool IsDelete
    {
        get { return Method == Bundle.HTTPVerb.DELETE; }
    }

    public bool IsPresent
    {
        get { return Method != Bundle.HTTPVerb.DELETE; }
    }

    public static Entry Post(IKey key, Resource? resource)
    {
        return Create(Bundle.HTTPVerb.POST, key, resource);
    }

    public static Entry Post(Resource resource)
    {
        return Create(Bundle.HTTPVerb.POST, resource);
    }

    public static Entry Put(IKey key, Resource? resource)
    {
        return Create(Bundle.HTTPVerb.PUT, key, resource);
    }

    public static Entry Patch(IKey key, Resource? resource)
    {
        return Create(Bundle.HTTPVerb.PATCH, key, resource);
    }

    public override string ToString()
    {
        return $"{Method} {Key}";
    }
}