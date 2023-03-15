// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;
using Task = System.Threading.Tasks.Task;

/// <summary>
///     Import can map id's and references in incoming entries to id's and references that are local to the Spark Server.
/// </summary>
internal class Import
{
    private readonly IGenerator _generator;
    private readonly ILocalhost _localhost;

    public Import(ILocalhost localhost, IGenerator generator)
    {
        _localhost = localhost;
        _generator = generator;
    }

    public async Task<Entry> Internalize(Entry entry, Mapper<string, IKey>? mapper = null, CancellationToken cancellationToken = default)
    {
        var m = mapper ?? new Mapper<string, IKey>();
        await InternalizeKey(entry, m, cancellationToken).ConfigureAwait(false);
        InternalizeReference(entry, m);
        InternalizeState(entry);
        return entry;
    }

    private static void InternalizeState(Entry entry)
    {
        if (entry.State == EntryState.Undefined)
        {
            entry.State = EntryState.Internal;
        }
    }

    private void InternalizeReference(Entry entry, Mapper<string, IKey> mapper)
    {
        if (entry is { State: EntryState.Undefined, Resource: { } })
        {
            InternalizeReferences(entry.Resource!, mapper);
        }
    }

    private async Task<IKey> Remap(Resource resource, Mapper<string, IKey> mapper, CancellationToken cancellationToken)
    {
        var newKey = await _generator.NextKey(resource, cancellationToken).ConfigureAwait(false);
        AddKeyToInternalMapping(resource.ExtractKey(), newKey.WithoutBase(), mapper);
        return newKey;
    }

    private async Task<IKey> RemapHistoryOnly(IKey key, Mapper<string, IKey> mapper, CancellationToken cancellationToken)
    {
        IKey newKey = await _generator.NextHistoryKey(key, cancellationToken).ConfigureAwait(false);
        AddKeyToInternalMapping(key, newKey.WithoutBase(), mapper);
        return newKey;
    }

    private void AddKeyToInternalMapping(IKey localKey, IKey generatedKey, Mapper<string, IKey> mapper)
    {
        mapper.Remap(
            _localhost.GetKeyKind(localKey) == KeyKind.Temporary ? localKey.ResourceId ?? throw new NullReferenceException("Missing resource id") : localKey.ToString()!,
            generatedKey.WithoutVersion());
    }

    private async Task InternalizeKey(Entry entry, Mapper<string, IKey> mapper, CancellationToken cancellationToken)
    {
        var key = entry.Key;
        if (key == null)
        {
            return;
        }
        switch (_localhost.GetKeyKind(key))
        {
            case KeyKind.Foreign:
            {
                if (entry.Resource != null)
                {
                    entry.Key = await Remap(entry.Resource, mapper, cancellationToken).ConfigureAwait(false);
                }

                return;
            }
            case KeyKind.Temporary:
            {
                if (entry.Resource != null)
                {
                    entry.Key = await Remap(entry.Resource, mapper, cancellationToken).ConfigureAwait(false);
                }

                return;
            }
            case KeyKind.Local:
            case KeyKind.Internal:
            {
                if (entry.Method is Bundle.HTTPVerb.PUT or Bundle.HTTPVerb.PATCH or Bundle.HTTPVerb.DELETE)
                {
                    entry.Key = await RemapHistoryOnly(key, mapper, cancellationToken).ConfigureAwait(false);
                }
                else if (entry is { Method: Bundle.HTTPVerb.POST, Resource: { } })
                {
                    entry.Key = await Remap(entry.Resource, mapper, cancellationToken).ConfigureAwait(false);
                }
                return;
            }
            default:
            {
                // switch can never get here.
                throw Error.Internal("Unexpected key for resource: " + entry.Key);
            }
        }
    }

    private void InternalizeReferences(Resource resource, Mapper<string, IKey> mapper)
    {
        void Visitor(Element element, string name)
        {
            switch (element)
            {
                case null:
                    return;
                case ResourceReference reference:
                {
                    if (reference.Url != null)
                    {
                        reference.Url = new Uri(
                            InternalizeReference(reference.Url.ToString(), mapper),
                            UriKind.RelativeOrAbsolute);
                    }

                    break;
                }
                case FhirUri uri:
                    uri.Value = InternalizeReference(uri.Value, mapper);
                    break;
                case Narrative n:
                    n.Div = FixXhtmlDiv(n.Div, mapper);
                    break;
            }
        }

        Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };

        Auxiliary.ResourceVisitor.VisitByType(resource, Visitor, types);
    }

    private IKey InternalizeReference(IKey localkey, Mapper<string, IKey> mapper)
    {
        var triage = _localhost.GetKeyKind(localkey);
        return triage switch
        {
            KeyKind.Foreign => throw new ArgumentException("Cannot internalize foreign reference"),
            KeyKind.Temporary => GetReplacement(localkey, mapper),
            _ => triage == KeyKind.Local ? localkey.WithoutBase() : localkey
        };
    }

    private IKey GetReplacement(IKey localKey, Mapper<string, IKey> mapper)
    {
        var replacement = localKey;
        //CCR: To check if this is still needed. Since we don't store the version in the mapper, do we ever need to replace the key multiple times?
        while (replacement?.ResourceId != null && mapper.Exists(replacement.ResourceId))
        {
            var triage = _localhost.GetKeyKind(localKey);
            replacement = mapper.TryGet(triage == KeyKind.Temporary ? replacement.ResourceId : replacement.ToString()!);
        }

        return replacement!;
    }

    private string InternalizeReference(string uristring, Mapper<string, IKey> mapper)
    {
        if (string.IsNullOrWhiteSpace(uristring))
        {
            return uristring;
        }

        var uri = new Uri(uristring, UriKind.RelativeOrAbsolute);

        // If it is a reference to another contained resource do not internalize.
        // BALLOT: this seems very... ad hoc.
        if (uri.HasFragment())
        {
            return uristring;
        }

        if (uri.IsTemporaryUri() || _localhost.IsBaseOf(uri))
        {
            IKey key = _localhost.UriToKey(uri);
            return InternalizeReference(key, mapper).ToUri().ToString();
        }

        return uristring;
    }

    private string FixXhtmlDiv(string div, Mapper<string, IKey> mapper)
    {
        try
        {
            var xdoc = XDocument.Parse(div);
            xdoc.VisitAttributes("img", "src", n => n.Value = InternalizeReference(n.Value, mapper));
            xdoc.VisitAttributes("a", "href", n => n.Value = InternalizeReference(n.Value, mapper));
            return xdoc.ToString();
        }
        catch
        {
            // illegal xml, don't bother, just return the argument
            // todo: should we really allow illegal xml ? /mh
            return div;
        }
    }
}