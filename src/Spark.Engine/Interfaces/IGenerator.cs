// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Interfaces
{
    using System.Threading.Tasks;
    using Hl7.Fhir.Model;

    public interface IGenerator
    {
        Task<string> NextResourceId(Resource resource);
        Task<string> NextVersionId(string resourceIdentifier);
        Task<string> NextVersionId(string resourceType, string resourceIdentifier);
    }
}