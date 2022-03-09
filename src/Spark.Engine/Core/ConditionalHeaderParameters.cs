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
    using System.Collections.Generic;
    using System.Linq;

    public class ConditionalHeaderParameters
    {
        public ConditionalHeaderParameters(IEnumerable<string> ifNoneMatchTags, DateTimeOffset? ifModifiedSince)
        {
            IfModifiedSince = ifModifiedSince;
            IfNoneMatchTags = ifNoneMatchTags.ToArray();
        }

        public IEnumerable<string> IfNoneMatchTags { get; }
        public DateTimeOffset? IfModifiedSince { get; }
    }
}