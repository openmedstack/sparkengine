// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System;

    public class HistoryParameters
    {
        public HistoryParameters(int? count, DateTimeOffset? since, string sortBy) //, string format = null)
        {
            Count = count;
            Since = since;
            SortBy = sortBy;
            //Format = format;
        }

        public int? Count { get; }

        public DateTimeOffset? Since { get; }

        //public string Format { get; }

        public string SortBy { get; }
    }
}