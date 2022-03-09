// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service
{
    using System;
    using System.Collections.Generic;
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
        private readonly List<Entry> _entries;
        private readonly IGenerator _generator;
        private readonly ILocalhost _localhost;
        private readonly Mapper<string, IKey> _mapper;

        public Import(ILocalhost localhost, IGenerator generator)
        {
            _localhost = localhost;
            _generator = generator;
            _mapper = new Mapper<string, IKey>();
            _entries = new List<Entry>();
        }

        public void Add(Entry interaction)
        {
            if (interaction is {State: EntryState.Undefined})
            {
                _entries.Add(interaction);
            }
        }

        public void AddMappings(Mapper<string, IKey> mappings)
        {
            _mapper.Merge(mappings);
        }

        public void Add(IEnumerable<Entry> interactions)
        {
            foreach (var interaction in interactions)
            {
                Add(interaction);
            }
        }

        public async Task Internalize()
        {
            await InternalizeKeys().ConfigureAwait(false);
            InternalizeReferences();
            InternalizeState();
        }

        private void InternalizeState()
        {
            foreach (var interaction in _entries.Transferable())
            {
                interaction.State = EntryState.Internal;
            }
        }

        private async Task InternalizeKeys()
        {
            foreach (var interaction in _entries.Transferable())
            {
                await InternalizeKey(interaction).ConfigureAwait(false);
            }
        }

        private void InternalizeReferences()
        {
            foreach (var entry in _entries.Transferable())
            {
                InternalizeReferences(entry.Resource);
            }
        }

        private async Task<IKey> Remap(Resource resource)
        {
            var newKey = await _generator.NextKey(resource).ConfigureAwait(false);
            AddKeyToInternalMapping(resource.ExtractKey(), newKey.WithoutBase());
            return newKey;
        }

        private async Task<IKey> RemapHistoryOnly(IKey key)
        {
            IKey newKey = await _generator.NextHistoryKey(key).ConfigureAwait(false);
            AddKeyToInternalMapping(key, newKey.WithoutBase());
            return newKey;
        }

        private void AddKeyToInternalMapping(IKey localKey, IKey generatedKey)
        {
            _mapper.Remap(
                _localhost.GetKeyKind(localKey) == KeyKind.Temporary ? localKey.ResourceId : localKey.ToString(),
                generatedKey.WithoutVersion());
        }

        private async Task InternalizeKey(Entry entry)
        {
            var key = entry.Key;

            switch (_localhost.GetKeyKind(key))
            {
                case KeyKind.Foreign:
                {
                    entry.Key = await Remap(entry.Resource).ConfigureAwait(false);
                    return;
                }
                case KeyKind.Temporary:
                {
                    entry.Key = await Remap(entry.Resource).ConfigureAwait(false);
                    return;
                }
                case KeyKind.Local:
                case KeyKind.Internal:
                {
                    if (entry.Method == Bundle.HTTPVerb.PUT || entry.Method == Bundle.HTTPVerb.PATCH || entry.Method == Bundle.HTTPVerb.DELETE)
                    {
                        entry.Key = await RemapHistoryOnly(key);
                    }
                    else if(entry.Method == Bundle.HTTPVerb.POST)
                    {
                        entry.Key = await Remap(entry.Resource);
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

        private void InternalizeReferences(Resource resource)
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
                                InternalizeReference(reference.Url.ToString()),
                                UriKind.RelativeOrAbsolute);
                        }

                        break;
                    }
                    case FhirUri uri:
                        uri.Value = InternalizeReference(uri.Value);
                        break;
                    case Narrative n:
                        n.Div = FixXhtmlDiv(n.Div);
                        break;
                }
            }

            Type[] types = {typeof(ResourceReference), typeof(FhirUri), typeof(Narrative)};

            Auxiliary.ResourceVisitor.VisitByType(resource, Visitor, types);
        }

        private IKey InternalizeReference(IKey localkey)
        {
            var triage = _localhost.GetKeyKind(localkey);
            return triage switch
            {
                KeyKind.Foreign => throw new ArgumentException("Cannot internalize foreign reference"),
                KeyKind.Temporary => GetReplacement(localkey),
                _ => triage == KeyKind.Local ? localkey.WithoutBase() : localkey
            };
        }

        private IKey GetReplacement(IKey localKey)
        {
            var replacement = localKey;
            //CCR: To check if this is still needed. Since we don't store the version in the mapper, do we ever need to replace the key multiple times?
            while (_mapper.Exists(replacement.ResourceId))
            {
                var triage = _localhost.GetKeyKind(localKey);
                replacement = _mapper.TryGet(triage == KeyKind.Temporary ? replacement.ResourceId : replacement.ToString());
            }

            return replacement;
        }

        private string InternalizeReference(string uristring)
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
                return InternalizeReference(key).ToUri().ToString();
            }

            return uristring;
        }

        private string FixXhtmlDiv(string div)
        {
            try
            {
                var xdoc = XDocument.Parse(div);
                xdoc.VisitAttributes("img", "src", n => n.Value = InternalizeReference(n.Value));
                xdoc.VisitAttributes("a", "href", n => n.Value = InternalizeReference(n.Value));
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
}