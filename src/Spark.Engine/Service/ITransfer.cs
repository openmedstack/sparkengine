// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;

    public interface ITransfer
    {
        void Externalize(IEnumerable<Entry> interactions);
        void Externalize(Entry interaction);
        Task Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper);
        Task Internalize(Entry entry);
    }
}