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

    public class Localhost : ILocalhost
    {
        public Localhost(Uri baseuri) => DefaultBase = baseuri;

        public Uri DefaultBase { get; set; }

        public Uri Absolute(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }

            var @base = DefaultBase.ToString().TrimEnd('/') + "/";
            return new Uri(@base + uri);
        }

        public bool IsBaseOf(Uri uri) => uri.IsAbsoluteUri && UriHelper.IsBaseOf(DefaultBase, uri);

        public Uri GetBaseOf(Uri uri) => IsBaseOf(uri) ? DefaultBase : null;
    }
}