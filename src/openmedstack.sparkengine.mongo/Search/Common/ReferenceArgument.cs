// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    public class ReferenceArgument : Argument
    {
        private string Groom(string value)
        {
            if (value != null)
            {
                //value = Regex.Replace(value, "/(?=[^@])", "/@"); // force include @ after "/", so "patient/10" becomes "patient/@10"
                return value.Trim();
            }

            return null;
        }

        public override string GroomElement(string value) => Groom(value);
    }
}