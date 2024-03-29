﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Tests.Core;

using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using SparkEngine.Core;
using Xunit;

public class ElementQueryTests
{
    [Fact]
    public void TestVisitOnePathZeroMatch()
    {
        var sut = new ElementQuery("Patient.name");

        var testPatient = new Patient();
        var result = new List<object?>();

        sut.Visit(testPatient, fd => result.Add(fd));

        Assert.Equal(testPatient.Name.Count, result.Count(ob => ob != null));
    }

    [Fact]
    public void TestVisitOnePathOneMatch()
    {
        var sut = new ElementQuery("Patient.name");

        var testPatient = new Patient();
        var hn = new HumanName().WithGiven("Sjors").AndFamily("Jansen");
        testPatient.Name = [hn];

        var result = new List<object?>();

        sut.Visit(testPatient, fd => result.Add(fd));

        Assert.Equal(testPatient.Name.Count, result.Count(ob => ob != null));
        Assert.Contains(hn, result);
    }

    [Fact]
    public void TestVisitOnePathTwoMatches()
    {
        var sut = new ElementQuery("Patient.name");

        var testPatient = new Patient();
        var hn1 = new HumanName().WithGiven("A").AndFamily("B");
        var hn2 = new HumanName().WithGiven("Y").AndFamily("Z");
        testPatient.Name = [hn1, hn2];

        var result = new List<object?>();

        sut.Visit(testPatient, fd => result.Add(fd));

        Assert.Equal(testPatient.Name.Count, result.Count(ob => ob != null));
        Assert.Contains(hn1, result);
        Assert.Contains(hn2, result);
    }
}
