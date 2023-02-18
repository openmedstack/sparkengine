// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Postgres
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Hl7.Fhir.Model;
    using Interfaces;
    using Task = System.Threading.Tasks.Task;

    public class GuidGenerator : IGenerator
    {
        /// <inheritdoc />
        public Task<string> NextResourceId(Resource resource, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid().ToString("N"));

        /// <inheritdoc />
        public Task<string> NextVersionId(string resourceIdentifier, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid().ToString("N"));

        /// <inheritdoc />
        public Task<string> NextVersionId(
            string resourceType,
            string resourceIdentifier,
            CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid().ToString("N"));
    }
}