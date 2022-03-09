// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    using Hl7.Fhir.Model;

    public static class ArgumentFactory
    {
        public static Argument Create(SearchParamType type)
        {
            return type switch
            {
                SearchParamType.Number => new IntArgument(),
                SearchParamType.String => new StringArgument(),
                SearchParamType.Date => new DateArgument(),
                SearchParamType.Token => new TokenArgument(),
                SearchParamType.Reference => new ReferenceArgument(),
                SearchParamType.Composite =>
                    //TODO: Implement Composite arguments
                    new Argument(),
                _ => new Argument()
            };
        }
    }
}