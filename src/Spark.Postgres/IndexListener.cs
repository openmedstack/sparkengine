// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Postgres
{
    using System;
    using System.Threading.Tasks;
    using Engine.Core;
    using Engine.Service;

    public class IndexListener : IServiceListener
    {
        private readonly IIndexService _index;

        public IndexListener(IIndexService index) => _index = index;

        /// <inheritdoc />
        public Task Inform(Uri location, Entry interaction) => _index.Process(interaction);
    }
}