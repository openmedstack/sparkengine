// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    using Engine.Core;
    using Hl7.Fhir.Model;

    public class Definition
    {
        public Argument Argument { get; set; }
        public string Resource { get; set; }
        public string ParamName { get; set; }
        public string Description { get; set; }
        public SearchParamType ParamType { get; set; }
        public ElementQuery Query { get; set; }

        public override string ToString() => $"{Resource.ToLower()}.{ParamName.ToLower()}->{Query}";
    }
}