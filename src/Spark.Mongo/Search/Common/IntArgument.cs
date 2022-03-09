// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    using Searcher;

    public class IntArgument : Argument
    {
        public override string GroomElement(string value) => value?.Trim();

        public override string ValueToString(ITerm term) => term.Operator + term.Value;

        public override bool Validate(string value)
        {
            return int.TryParse(value, out _);
        }
    }
}