// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System;
    using Extensions;
    using Hl7.Fhir.Rest;

    public static class LocalhostExtensions
    {
        public static bool IsLocal(this ILocalhost localhost, IKey key) =>
            key.Base == null || localhost.IsBaseOf(key.Base);

        public static Uri RemoveBase(this ILocalhost localhost, Uri uri)
        {
            var @base = localhost.GetBaseOf(uri)?.ToString();
            if (@base == null)
            {
                return uri;
            }

            var s = uri.ToString();
            var path = s.Remove(0, @base.Length);
            return new Uri(path, UriKind.Relative);
        }

        public static Key LocalUriToKey(this ILocalhost localhost, Uri uri)
        {
            var s = uri.ToString();
            var @base = localhost.GetBaseOf(uri)?.ToString();
            var path = s.Remove(0, @base?.Length ?? 0);

            return Key.ParseOperationPath(path).WithBase(@base);
        }

        public static Key UriToKey(this ILocalhost localhost, Uri uri)
        {
            if (uri.IsAbsoluteUri && uri.IsTemporaryUri() == false)
            {
                return localhost.IsBaseOf(uri)
                    ? localhost.LocalUriToKey(uri)
                    : throw new ArgumentException("Cannot create a key from a foreign Uri");
            }

            if (uri.IsTemporaryUri())
            {
                return Key.Create(null, uri.ToString());
            }

            var path = uri.ToString();
            return Key.ParseOperationPath(path);
        }

        public static Key UriToKey(this ILocalhost localhost, string uristring)
        {
            var uri = new Uri(uristring, UriKind.RelativeOrAbsolute);
            return localhost.UriToKey(uri);
        }

        public static Uri GetAbsoluteUri(this ILocalhost localhost, IKey key) => key.ToUri(localhost.DefaultBase);

        public static KeyKind GetKeyKind(this ILocalhost localhost, IKey key)
        {
            if (key.IsTemporary())
            {
                return KeyKind.Temporary;
            }

            if (!key.HasBase())
            {
                return KeyKind.Internal;
            }

            return localhost.IsLocal(key) ? KeyKind.Local : KeyKind.Foreign;
        }

        private static bool IsBaseOf(this ILocalhost localhost, string uristring)
        {
            var uri = new Uri(uristring, UriKind.RelativeOrAbsolute);
            return localhost.IsBaseOf(uri);
        }

        public static Uri Uri(this ILocalhost localhost, params string[] segments) =>
            new RestUrl(localhost.DefaultBase).AddPath(segments).Uri;
    }
}