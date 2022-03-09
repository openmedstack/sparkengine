/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Hl7.Fhir.Model;

    public interface ITransactionService
    {
        Task<FhirResponse> HandleTransaction(ResourceManipulationOperation operation, IInteractionHandler interactionHandler);
        IAsyncEnumerable<Tuple<Entry, FhirResponse>> HandleTransaction(Bundle bundle, IInteractionHandler interactionHandler);
        IAsyncEnumerable<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions, IInteractionHandler interactionHandler);
    }
}