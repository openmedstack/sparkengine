﻿// /*
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

    /// <summary>
    ///     DateTimeValue is allways specified up to the second.
    ///     Spark uses it for the boundaries of a period. So fuzzy dates as in FhirDateTime (just year + month for example) get
    ///     translated in an upper- and lowerbound in DateTimeValues.
    ///     These are used for indexing.
    /// </summary>
    public class DateTimeValue : ValueExpression
    {
        public DateTimeValue(DateTimeOffset value) =>
            // The DateValue datatype is not interested in any time related
            // components, so we must strip those off before converting to the string
            // value
            Value = value;

        public DateTimeValue(string datetime)
        {
            if (!FhirDateTime.IsValidValue(datetime))
            {
                throw Error.Argument(
                    "datetime",
                    "The string [" + datetime + "] cannot be translated to a DateTimeValue");
            }

            var fdt = new FhirDateTime(datetime);
            Value = fdt.ToDateTimeOffset(TimeSpan.Zero);
        }

        public DateTimeOffset Value { get; }

        public override string ToString() => new FhirDateTime(Value).ToString();
    }
}