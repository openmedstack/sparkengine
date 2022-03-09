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

    public static class UriParamExtensions
    {
        //TODO: horrible!! Should refactor
        public static Uri AddParam(this Uri uri, string name, params string[] values)
        {
            var fakeBase = new Uri("http://example.com");
            var builder = uri.IsAbsoluteUri ? new UriBuilder(uri) : new UriBuilder(fakeBase) {Path = uri.ToString()};

            ICollection<Tuple<string, string>> query = UriUtil.SplitParams(builder.Query).ToList();

            foreach (var value in values)
            {
                query.Add(new Tuple<string, string>(name, value));
            }

            builder.Query = UriUtil.JoinParams(query);

            return uri.IsAbsoluteUri ? builder.Uri : fakeBase.MakeRelativeUri(builder.Uri);
        }
    }
}