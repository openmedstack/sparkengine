// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    public interface IIndexBuildProgressReporter
    {
        Task ReportProgress(int progress, string message);

        Task ReportError(string message);
    }
}