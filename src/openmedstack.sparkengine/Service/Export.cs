// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service;

using System;
using System.Xml.Linq;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;

/// <summary>
///     Import can map id's and references  that are local to the Spark Server to absolute id's and references in outgoing
///     Interactions.
/// </summary>
internal class Export
{
    private readonly ExportSettings _exportSettings;
    private readonly ILocalhost _localhost;

    public Export(ILocalhost localhost, ExportSettings exportSettings)
    {
        _localhost = localhost;
        _exportSettings = exportSettings;
    }

    public Entry Externalize(Entry entry)
    {
        ExternalizeKey(entry);
        if (entry.Resource != null)
        {
            ExternalizeReferences(entry.Resource);
        }
        ExternalizeState(entry);
        return entry;
    }

    private void ExternalizeState(Entry entry)
    {
        entry.State = EntryState.External;
    }

    private void ExternalizeKey(Entry entry)
    {
        entry.SupplementBase(_localhost.DefaultBase);
    }

    private void ExternalizeReferences(Resource resource)
    {
        void Action(Element element, string name)
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
                            ExternalizeReference(reference.Url.ToString()),
                            UriKind.RelativeOrAbsolute);
                    }

                    break;
                }
                case FhirUri uri:
                    uri.Value = ExternalizeReference(uri.Value);
                    //((FhirUri)element).Value = LocalizeReference(new Uri(((FhirUri)element).Value, UriKind.RelativeOrAbsolute)).ToString();
                    break;
                case Narrative n:
                    n.Div = FixXhtmlDiv(n.Div);
                    break;
            }
        }

        Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };

        Auxiliary.ResourceVisitor.VisitByType(resource, Action, types);
    }

    private string ExternalizeReference(string uristring)
    {
        if (string.IsNullOrWhiteSpace(uristring))
        {
            return uristring;
        }

        var uri = new Uri(uristring, UriKind.RelativeOrAbsolute);

        switch (uri.IsAbsoluteUri)
        {
            case false when _exportSettings.ExternalizeFhirUri:
            {
                var absoluteUri = _localhost.Absolute(uri);
                return absoluteUri.Fragment == uri.ToString() ? uristring : absoluteUri.ToString();
            }
            default:
                return uristring;
        }
    }

    private string FixXhtmlDiv(string div)
    {
        try
        {
            var xdoc = XDocument.Parse(div);
            xdoc.VisitAttributes("img", "src", n => n.Value = ExternalizeReference(n.Value));
            xdoc.VisitAttributes("a", "href", n => n.Value = ExternalizeReference(n.Value));
            return xdoc.ToString();
        }
        catch
        {
            // illegal xml, don't bother, just return the argument
            // todo: should we really allow illegal xml ? /mh
            return div;
        }
    }

    /*
    public void RemoveBodyFromEntries(List<Entry> entries)
    {
        foreach (Entry entry in entries)
        {
            if (entry.IsResource())
            {
                entry.Resource = null;
            }
        }
    }
    */
}