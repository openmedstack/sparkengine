// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Hl7.Fhir.Model;

    public static class EntryExtensions
    {
        public static Key ExtractKey(this ILocalhost localhost, Bundle.EntryComponent entry)
        {
            Key key = null;
            if (entry.Request?.Url != null)
            {
                key = localhost.UriToKey(entry.Request.Url);
            }
            else if (entry.Resource != null)
            {
                key = entry.Resource.ExtractKey();
            }

            if (key != null
                && string.IsNullOrEmpty(key.ResourceId)
                && entry.FullUrl != null
                && UriHelper.IsTemporaryUri(entry.FullUrl))
            {
                key.ResourceId = entry.FullUrl;
            }

            return key;
        }

        private static Bundle.HTTPVerb DetermineMethod(ILocalhost localhost, IKey key)
        {
            if (key == null)
            {
                return Bundle.HTTPVerb.DELETE; // probably...
            }

            return localhost.GetKeyKind(key) switch
            {
                KeyKind.Foreign => Bundle.HTTPVerb.POST,
                KeyKind.Temporary => Bundle.HTTPVerb.POST,
                KeyKind.Internal => Bundle.HTTPVerb.PUT,
                KeyKind.Local => Bundle.HTTPVerb.PUT,
                _ => Bundle.HTTPVerb.PUT
            };
        }

        public static Bundle.HTTPVerb ExtrapolateMethod(
            this ILocalhost localhost,
            Bundle.EntryComponent entry,
            IKey key) =>
            entry.Request?.Method ?? DetermineMethod(localhost, key);
        
        public static Bundle.EntryComponent TranslateToSparseEntry(this Entry entry, FhirResponse response = null)
        {
            var bundleEntry = new Bundle.EntryComponent();
            if (response != null)
            {
                bundleEntry.Response = new Bundle.ResponseComponent
                {
                    Status = $"{(int) response.StatusCode} {response.StatusCode}",
                    Location = response.Key?.ToString(),
                    Etag = response.Key != null ? ETag.Create(response.Key.VersionId).ToString() : null,
                    LastModified = entry?.Resource?.Meta?.LastUpdated
                };
            }

            SetBundleEntryResource(entry, bundleEntry);
            return bundleEntry;
        }

        public static Bundle.EntryComponent ToTransactionEntry(this Entry entry)
        {
            var bundleEntry = new Bundle.EntryComponent();

            bundleEntry.Request ??= new Bundle.RequestComponent();
            bundleEntry.Request.Method = entry.Method;
            bundleEntry.Request.Url = entry.Key.ToUri().ToString();

            SetBundleEntryResource(entry, bundleEntry);

            return bundleEntry;
        }

        private static void SetBundleEntryResource(Entry entry, Bundle.EntryComponent bundleEntry)
        {
            if (entry.HasResource())
            {
                bundleEntry.Resource = entry.Resource;
                entry.Key.ApplyTo(bundleEntry.Resource);
                bundleEntry.FullUrl = entry.Key.ToUriString();
            }
        }

        public static bool HasResource(this Entry entry) => entry.Resource != null;

        public static bool IsDeleted(this Entry entry) =>
            // API: HTTPVerb should have a broader scope than Bundle.
            entry.Method == Bundle.HTTPVerb.DELETE;

        public static void Append(this IList<Entry> list, IList<Entry> appendage)
        {
            foreach (var entry in appendage)
            {
                list.Add(entry);
            }
        }

        public static void AppendDistinct(this IList<Entry> list, IList<Entry> appendage)
        {
            foreach (var item in appendage)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }

        public static IEnumerable<Resource> GetResources(this IEnumerable<Entry> entries)
        {
            return entries.Where(i => i.HasResource()).Select(i => i.Resource);
        }

        private static bool IsValidResourcePath(string path, Resource resource)
        {
            var name = path.Split('.').FirstOrDefault();
            return resource.TypeName == name;
        }

        public static IEnumerable<string> GetReferences(this Resource resource, string path)
        {
            if (!IsValidResourcePath(path, resource))
            {
                return Enumerable.Empty<string>();
            }

            var query = new ElementQuery(path);
            var list = new List<string>();

            query.Visit(
                resource,
                element =>
                {
                    if (!(element is ResourceReference resourceReference))
                    {
                        return;
                    }

                    var reference = resourceReference.Reference;
                    if (reference != null)
                    {
                        list.Add(reference);
                    }
                });
            return list;
        }

        public static IEnumerable<string> GetReferences(this IEnumerable<Resource> resources, string path)
        {
            return resources.SelectMany(r => r.GetReferences(path));
            //foreach (Resource entry in resources)
            //{
            //    IEnumerable<string> list = GetLocalReferences(entry, include);
            //    foreach (Uri value in list)
            //    {
            //        if (value != null)
            //            yield return value;
            //    }
            //}
        }

        public static IEnumerable<string> GetReferences(this IEnumerable<Resource> resources, IEnumerable<string> paths)
        {
            return paths.SelectMany(resources.GetReferences);
        }

        // If an interaction has no base, you should be able to supplement it (from the containing bundle for example)
        public static void SupplementBase(this Entry entry, string @base)
        {
            var key = entry.Key.Clone();
            if (!key.HasBase())
            {
                key.Base = @base;
                entry.Key = key;
            }
        }

        public static void SupplementBase(this Entry entry, Uri @base)
        {
            SupplementBase(entry, @base.ToString());
        }

        public static IEnumerable<Entry> Transferable(this IEnumerable<Entry> entries)
        {
            return entries.Where(i => i.State == EntryState.Undefined);
        }
    }
}