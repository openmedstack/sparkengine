/*
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;
    using Core;

    public interface IInteractionHandler
    {
        Task<FhirResponse> HandleInteraction(Entry interaction);
    }
}
