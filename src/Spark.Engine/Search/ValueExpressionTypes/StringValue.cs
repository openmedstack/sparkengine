// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    public class StringValue : ValueExpression
    {
        public StringValue(string value) => Value = value;

        public string Value { get; }

        public override string ToString() => EscapeString(Value);

        public static StringValue Parse(string text) => new(UnescapeString(text));


        public static string EscapeString(string value)
        {
            if (value == null)
            {
                return null;
            }

            value = value.Replace(@"\", @"\\");
            value = value.Replace(@"$", @"\$");
            value = value.Replace(@",", @"\,");
            value = value.Replace(@"|", @"\|");

            return value;
        }

        public static string UnescapeString(string value)
        {
            if (value == null)
            {
                return null;
            }

            value = value.Replace(@"\|", @"|");
            value = value.Replace(@"\,", @",");
            value = value.Replace(@"\$", @"$");
            value = value.Replace(@"\\", @"\");

            return value;
        }
    }
}