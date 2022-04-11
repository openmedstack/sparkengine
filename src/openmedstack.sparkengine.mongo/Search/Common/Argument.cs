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

    public class Argument
    {
        public virtual string GroomElement(string value) => value;

        public virtual string ValueToString(ITerm term) => term.Value;

        public virtual string FieldToString(ITerm term) =>
            term.Operator != null ? term.Field + ":" + term.Operator : term.Field;

        public virtual bool Validate(string value) => true;

        private static string FieldToInternalField(string field)
        {
            if (Config.Equal(field, UniversalField.ID))
            {
                field = InternalField.JUSTID;
            }

            return field;
        }
    }
}