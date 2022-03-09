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

    public static class UriHelper
    {
        public static bool IsTemporaryUri(this Uri uri) => uri != null && IsTemporaryUri(uri.ToString());

        public static bool IsTemporaryUri(string uri) =>
            uri.StartsWith("urn:uuid:") || uri.StartsWith("urn:guid:") || uri.StartsWith("cid:");

        /// <summary>
        ///     Determines whether the uri contains a hash (#) fragment.
        /// </summary>
        public static bool HasFragment(this Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                var fragment = uri.Fragment;
                return !string.IsNullOrEmpty(fragment);
            }

            var s = uri.ToString();
            return s.StartsWith("#");
        }

        /// <summary>
        ///     Bug fixed_IsBaseOf is a fix for Uri.IsBaseOf which has a bug
        /// </summary>
        public static bool IsBaseOf(this Uri @base, Uri uri)
        {
            var b = @base.ToString().ToLowerInvariant();
            var u = uri.ToString().ToLowerInvariant();

            return u.StartsWith(b);
        }
    }
}