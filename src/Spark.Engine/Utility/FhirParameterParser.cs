// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Utility
{
    using System;

    public static class FhirParameterParser
    {
        public static DateTimeOffset? ParseDateParameter(this string value) =>
            DateTimeOffset.TryParse(value, out var dateTime) ? dateTime : (DateTimeOffset?) null;

        public static int? ParseIntParameter(this string value) => int.TryParse(value, out var n) ? n : default(int?);
    }
}