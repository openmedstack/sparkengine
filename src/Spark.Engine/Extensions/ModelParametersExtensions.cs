// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Hl7.Fhir.Model;

    public static class ModelParametersExtensions
    {
        public static IEnumerable<Meta> ExtractMeta(this Parameters parameters)
        {
            foreach (var parameter in parameters.Parameter.Where(p => p.Name == "meta"))
            {
                if (parameter.Value is Meta meta)
                {
                    yield return meta;
                }
            }
        }
    }
}