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
    using Interfaces;

    /// <summary>
    ///     Transfer maps between local id's and references and absolute id's and references upon incoming or outgoing
    ///     Interactions.
    ///     It uses an Import or Export to do de actual work for incoming or outgoing Interactions respectively.
    /// </summary>
    public class Transfer : ITransfer
    {
        private readonly IGenerator _generator;
        private readonly ILocalhost _localhost;
        private readonly SparkSettings _sparkSettings;

        public Transfer(IGenerator generator, ILocalhost localhost, SparkSettings sparkSettings = null)
        {
            _generator = generator;
            _localhost = localhost;
            _sparkSettings = sparkSettings;
        }

        public Task Internalize(Entry entry)
        {
            var import = new Import(_localhost, _generator);
            import.Add(entry);
            return import.Internalize();
        }


        public Task Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper = null)
        {
            var import = new Import(_localhost, _generator);
            if (mapper != null)
            {
                import.AddMappings(mapper);
            }

            import.Add(interactions);
            return import.Internalize();
        }

        public void Externalize(Entry interaction)
        {
            var export = new Export(_localhost, _sparkSettings?.ExportSettings ?? new ExportSettings());
            export.Add(interaction);
            export.Externalize();
        }

        public void Externalize(IEnumerable<Entry> interactions)
        {
            var export = new Export(_localhost, _sparkSettings?.ExportSettings ?? new ExportSettings());
            export.Add(interactions);
            export.Externalize();
        }
    }
}