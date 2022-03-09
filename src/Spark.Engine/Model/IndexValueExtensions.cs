// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Model
{
    using System.Collections.Generic;
    using System.Linq;

    public static class IndexValueExtensions
    {
        public static IEnumerable<IndexValue> IndexValues(this IndexValue root)
        {
            return root.Values.Where(v => v is IndexValue).Select(v => (IndexValue) v);
        }
    }
}