// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Service
{
    using System.Collections.Generic;
    using System.Linq;
    using Engine.Model;

    public static class IndexValueTestExtensions
    {
        public static IEnumerable<IndexValue> NonInternalValues(this IndexValue root)
        {
            return root.IndexValues().Where(v => !v.Name.StartsWith("internal_"));
        }
    }
}