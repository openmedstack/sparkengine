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
    using Core;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;

    public static class KeyExtensions
    {
        public static Key ExtractKey(this Resource resource)
        {
            var @base = resource.ResourceBase?.ToString();
            var key = new Key(@base, resource.TypeName, resource.Id, resource.VersionId);
            return key;
        }

        public static Key ExtractKey(Uri uri)
        {
            var identity = new ResourceIdentity(uri);

            var @base = identity.HasBaseUri ? identity.BaseUri.ToString() : null;
            var key = new Key(@base, identity.ResourceType, identity.Id, identity.VersionId);
            return key;
        }

        //public static Key ExtractKey(this Localhost localhost, Bundle.EntryComponent entry)
        //{
        //    var uri = new Uri(entry.Request.Url, UriKind.RelativeOrAbsolute);
        //    var compare = ExtractKey(uri); // This fails!! ResourceIdentity does not work in this case.
        //    return localhost.LocalUriToKey(uri);

        //}

        public static void ApplyTo(this IKey key, Resource resource)
        {
            resource.ResourceBase = key.HasBase() ? new Uri(key.Base) : null;
            resource.Id = key.ResourceId;
            resource.VersionId = key.VersionId;
        }

        public static Key Clone(this IKey self)
        {
            var key = new Key(self.Base, self.TypeName, self.ResourceId, self.VersionId);
            return key;
        }

        public static bool HasBase(this IKey key) => !string.IsNullOrEmpty(key.Base);

        public static Key WithBase(this IKey self, string @base)
        {
            var key = self.Clone();
            key.Base = @base;
            return key;
        }

        public static Key WithoutBase(this IKey self)
        {
            var key = self.Clone();
            key.Base = null;
            return key;
        }

        public static Key WithoutVersion(this IKey self)
        {
            var key = self.Clone();
            key.VersionId = null;
            return key;
        }

        public static bool HasVersionId(this IKey self) => !string.IsNullOrEmpty(self.VersionId);

        public static bool HasResourceId(this IKey self) => !string.IsNullOrEmpty(self.ResourceId);

        public static IKey WithoutResourceId(this IKey self)
        {
            var key = self.Clone();
            key.ResourceId = null;
            return key;
        }

        /// <summary>
        ///     If an id is provided, the server SHALL ignore it.
        ///     If the request body includes a meta, the server SHALL ignore
        ///     the existing versionId and lastUpdated values.
        ///     http://hl7.org/fhir/STU3/http.html#create
        ///     http://hl7.org/fhir/R4/http.html#create
        /// </summary>
        public static IKey CleanupForCreate(this IKey key)
        {
            if (key.HasResourceId())
            {
                key = key.WithoutResourceId();
            }

            if (key.HasVersionId())
            {
                key = key.WithoutVersion();
            }

            return key;
        }

        public static IEnumerable<string> GetSegments(this IKey key)
        {
            if (key.Base != null)
            {
                yield return key.Base;
            }

            if (key.TypeName != null)
            {
                yield return key.TypeName;
            }

            if (key.ResourceId != null)
            {
                yield return key.ResourceId;
            }

            if (key.VersionId != null)
            {
                yield return "_history";
                yield return key.VersionId;
            }
        }

        public static string ToUriString(this IKey key)
        {
            var segments = key.GetSegments();
            return string.Join("/", segments);
        }

        public static string ToOperationPath(this IKey self)
        {
            var key = self.WithoutBase();
            return key.ToUriString();
        }

        /// <summary>
        ///     A storage key is a resource reference string that is ensured to be server wide unique.
        ///     This way resource can refer to eachother at a database level.
        ///     These references are also used in SearchResult lists.
        ///     The format is "resource/id/_history/vid"
        /// </summary>
        /// <returns>a string</returns>
        public static string ToStorageKey(this IKey key) => key.WithoutBase().ToUriString();

        public static Uri ToRelativeUri(this IKey key)
        {
            var path = key.ToOperationPath();
            return new Uri(path, UriKind.Relative);
        }

        public static Uri ToUri(this IKey self) => new(self.ToUriString(), UriKind.RelativeOrAbsolute);

        public static Uri ToUri(this IKey key, Uri endpoint)
        {
            var @base = endpoint.ToString().TrimEnd('/');
            var s = $"{@base}/{key}";
            return new Uri(s);
        }

        /// <summary>
        ///     Determines if the Key was constructed from a temporary id.
        /// </summary>
        public static bool IsTemporary(this IKey key) =>
            key.ResourceId != null && UriHelper.IsTemporaryUri(key.ResourceId);
    }
}