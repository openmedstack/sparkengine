// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Interfaces;

using Core;

public interface IFhirResponseInterceptor
{
    FhirResponse? GetFhirResponse(ResourceInfo entry, object input);

    bool CanHandle(object input);
}