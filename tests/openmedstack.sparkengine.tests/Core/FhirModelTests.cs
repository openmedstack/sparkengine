// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Tests.Core;

using Hl7.Fhir.Model;
using SparkEngine.Core;
using Xunit;

public class FhirModelTests
{
    private static FhirModel _sut;

    public FhirModelTests() => _sut = new FhirModel();

    [Fact]
    public void TestCompartments()
    {
        var actual = _sut.FindCompartmentInfo(ResourceType.Patient);

        Assert.NotNull(actual);
        Assert.NotEmpty(actual.ReverseIncludes);
    }
}