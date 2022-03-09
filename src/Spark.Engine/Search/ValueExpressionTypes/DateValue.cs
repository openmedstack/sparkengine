// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    using System;
    using Hl7.Fhir.Model;
    using Support;

    public class DateValue : ValueExpression
    {
        public DateValue(DateTimeOffset value) =>
            // The DateValue datatype is not interested in any time related
            // components, so we must strip those off before converting to the string
            // value
            Value = value.Date.ToString("yyyy-MM-dd");

        public DateValue(string date)
        {
            if (!Date.IsValidValue(date))
            {
                if (!FhirDateTime.IsValidValue(date))
                {
                    throw Error.Argument(
                        "date",
                        "The string [" + date + "] is not a valid FHIR date string and isn't a FHIR datetime either");
                }

                // This was a time, so we can just use the date portion of this
                date = new FhirDateTime(date).ToDateTimeOffset(TimeSpan.Zero).Date.ToString("yyyy-MM-dd");
            }

            Value = date;
        }

        public string Value { get; }

        public override string ToString() => Value;

        public static DateValue Parse(string text) => new(text);
    }
}