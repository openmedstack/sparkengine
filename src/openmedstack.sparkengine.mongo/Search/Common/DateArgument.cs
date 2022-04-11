// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    using System.Text.RegularExpressions;

    public class DateArgument : Argument
    {
        private string Groom(string value)
        {
            if (value != null)
            {
                var s = Regex.Replace(value, @"[T\s:\-]", "");
                var i = s.IndexOf('+');
                if (i > 0)
                {
                    s = s.Remove(i);
                }

                return s;
            }

            return null;
        }

        public override string GroomElement(string value) => Groom(value);
    }
}