// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System;
    using Hl7.Fhir.Model;

    public static class FhirDateTimeExtensions
    {
        public enum FhirDateTimePrecision
        {
            Year = 4, //1994
            Month = 7, //1994-10
            Day = 10, //1994-10-21
            Minute = 15, //1994-10-21T13:45
            Second = 18 //1994-10-21T13:45:21
        }

        public static FhirDateTimePrecision Precision(this FhirDateTime fdt) =>
            (FhirDateTimePrecision) Math.Min(fdt.Value.Length, 18); //Ignore timezone for stating precision.

        public static DateTimeOffset LowerBound(this FhirDateTime fdt) => fdt.ToDateTimeOffset(TimeSpan.Zero);

        public static DateTimeOffset UpperBound(this FhirDateTime fdt)
        {
            var dtoStart = fdt.LowerBound();
            var dtoEnd = fdt.Precision() switch
            {
                FhirDateTimePrecision.Year => dtoStart.AddYears(1),
                FhirDateTimePrecision.Month => dtoStart.AddMonths(1),
                FhirDateTimePrecision.Day => dtoStart.AddDays(1),
                FhirDateTimePrecision.Minute => dtoStart.AddMinutes(1),
                FhirDateTimePrecision.Second => dtoStart.AddSeconds(1),
                _ => dtoStart
            };

            return dtoEnd;
        }
    }
}