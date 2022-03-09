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
    using Engine.Interfaces;
    using Hl7.Fhir.Model;
    using Task = System.Threading.Tasks.Task;

    public class GuidGenerator : IGenerator
    {
        /// <inheritdoc />
        public Task<string> NextResourceId(Resource resource) => Task.FromResult(Guid.NewGuid().ToString("N"));

        /// <inheritdoc />
        public Task<string> NextVersionId(string resourceIdentifier) => Task.FromResult(Guid.NewGuid().ToString("N"));

        /// <inheritdoc />
        public Task<string> NextVersionId(string resourceType, string resourceIdentifier) =>
            Task.FromResult(Guid.NewGuid().ToString("N"));
    }
}