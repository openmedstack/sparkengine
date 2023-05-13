// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Web;

using Controllers;
using Interfaces;
using Microsoft.AspNetCore.Mvc;

[Route("fhir")]
public class DefaultFhirController : FhirController
{
    /// <inheritdoc />
    public DefaultFhirController(IFhirService fhirService, IFhirModel model)
        : base(fhirService, model)
    {
    }
}
