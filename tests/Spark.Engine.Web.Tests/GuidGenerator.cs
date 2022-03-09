namespace Spark.Engine.Web.Tests
{
    using System;
    using System.Threading.Tasks;
    using Hl7.Fhir.Model;
    using Interfaces;
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