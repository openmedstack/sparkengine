// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Mongo.Search.Common
{
    using System.Collections.Generic;

    /*
    Ik heb deze class losgetrokken van SearchParamDefinition,
    omdat Definition onafhankelijk van Spark zou moeten kunnen bestaan.
    Er komt dus een converter voor in de plaats. -mh
    */

    public class Definitions
    {
        private readonly List<Definition> _definitions = new List<Definition>();

        public void Add(Definition definition)
        {
            _definitions.Add(definition);
        }

        public void Replace(Definition definition)
        {
            _definitions.RemoveAll(d => d.Resource == definition.Resource && d.ParamName == definition.ParamName);
            _definitions.Add(definition);
            // for manual correction
        }
    }
}