// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Engine.Core;
    using Hl7.Fhir.Model;
    using Xunit;

    public class ResourceVisitorTests : IDisposable
    {
        private readonly IFhirModel _fhirModel;
        //old version, with [x=y] as predicate
        //private Regex headTailRegex = new Regex(@"(?([^\.]*\[.*])(?<head>[^\[]*)\[(?<predicate>.*)](\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");

        //new version, with (x=y) as predicate (so with round brackets instead of square brackets.
        private readonly Regex _headTailRegex = new(
            @"(?([^\.]*\(.*\))(?<head>[^\(]*)\((?<predicate>.*)\)(\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");

        private readonly FhirPropertyIndex _index;
        private readonly Patient _patient;
        private readonly ResourceVisitor _sut;
        private int _actualActionCounter;
        private int _expectedActionCounter;

        public ResourceVisitorTests()
        {
            _fhirModel = new FhirModel();
            _index = new FhirPropertyIndex(
                _fhirModel,
                new List<Type>
                {
                    typeof(Patient),
                    typeof(ClinicalImpression),
                    typeof(HumanName),
                    typeof(CodeableConcept),
                    typeof(Coding)
                });
            _sut = new ResourceVisitor(_index);
            _patient = new Patient();
            _patient.Name.Add(new HumanName().WithGiven("Sjors").AndFamily("Jansen"));
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Assert.Equal(_expectedActionCounter, _actualActionCounter);
        }

        [Fact]
        public void TestHeadNoTail()
        {
            var test = "a";
            var match = _headTailRegex.Match(test);
            Assert.Equal("a", match.Groups["head"].Value);
            Assert.Equal("", match.Groups["predicate"].Value);
            Assert.Equal("", match.Groups["tail"].Value);
        }

        [Fact]
        public void TestHeadAndTailMultipleCharacters()
        {
            var test = "ax.bx.cx";
            var match = _headTailRegex.Match(test);
            Assert.Equal("ax", match.Groups["head"].Value);
            Assert.Equal("", match.Groups["predicate"].Value);
            Assert.Equal("bx.cx", match.Groups["tail"].Value);
        }

        [Fact]
        public void TestHeadWithPredicateNoTail()
        {
            var test = "a(x=y)";
            var match = _headTailRegex.Match(test);
            Assert.Equal("a", match.Groups["head"].Value);
            Assert.Equal("x=y", match.Groups["predicate"].Value);
            Assert.Equal("", match.Groups["tail"].Value);
        }

        [Fact]
        public void TestHeadAndTailNoPredicate()
        {
            var test = "a.b.c";
            var match = _headTailRegex.Match(test);
            Assert.Equal("a", match.Groups["head"].Value);
            Assert.Equal("", match.Groups["predicate"].Value);
            Assert.Equal("b.c", match.Groups["tail"].Value);
        }

        [Fact]
        public void TestHeadAndTailWithPredicate()
        {
            var test = "a(x.y=z).b.c";
            var match = _headTailRegex.Match(test);
            Assert.Equal("a", match.Groups["head"].Value);
            Assert.Equal("x.y=z", match.Groups["predicate"].Value);
            Assert.Equal("b.c", match.Groups["tail"].Value);
        }

        [Fact]
        public void TestLongerHeadAndTailWithPredicate()
        {
            var test = "ax(yx=zx).bx";
            var match = _headTailRegex.Match(test);
            Assert.Equal("ax", match.Groups["head"].Value);
            Assert.Equal("yx=zx", match.Groups["predicate"].Value);
            Assert.Equal("bx", match.Groups["tail"].Value);
        }

        [Fact]
        public void TestVisitNotExistingPathNoPredicate()
        {
            var result = true;
            _sut.VisitByPath(_patient, ob => { result = false; }, "not_existing_property");

            Assert.True(result);
        }

        [Fact]
        public void TestVisitSinglePathNoPredicate()
        {
            _expectedActionCounter = 1;
            _sut.VisitByPath(
                _patient,
                ob =>
                {
                    _actualActionCounter++;
                    if (ob.GetType() != typeof(HumanName))
                    {
                        throw new Exception("Failed test");
                    }
                },
                "name");
        }

        [Fact]
        public void TestVisitDataChoiceProperty()
        {
            _expectedActionCounter = 1;
            var ci = new ClinicalImpression {Code = new CodeableConcept("test.system", "test.code")};
            _sut.VisitByPath(
                ci,
                ob =>
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "test.system")
                    {
                        throw new Exception("Test fail");
                    }
                },
                "code.coding.system");
        }

        [Fact]
        public void TestVisitDataChoice_x_Property()
        {
            _expectedActionCounter =
                0; //We expect 0 actions: ResourceVisitor needs not recognize this, it should be solved in processing the searchparameter at indexing time.
            var cd = new Condition {Onset = new FhirDateTime(2015, 6, 15)};
            _sut.VisitByPath(
                cd,
                ob =>
                {
                    _actualActionCounter++;
                    if (ob.GetType() != typeof(FhirDateTime))
                    {
                        throw new Exception("Failed test");
                    }
                },
                "onset[x]");
        }

        [Fact]
        public void TestVisitNestedPathNoPredicate()
        {
            _expectedActionCounter = 1;
            _sut.VisitByPath(
                _patient,
                ob =>
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "Sjors")
                    {
                        throw new Exception("Failed test");
                    }
                },
                "name.given");
        }

        [Fact]
        public void TestVisitSinglePathWithPredicateAndFollowingProperty()
        {
            _expectedActionCounter = 1;
            _patient.Name.Add(new HumanName().WithGiven("Sjimmie").AndFamily("Visser"));
            _sut.VisitByPath(
                _patient,
                ob =>
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "Sjimmie")
                    {
                        throw new Exception("Failed test");
                    }
                },
                "name[given=Sjimmie].given");
        }

        [Fact]
        public void TestVisitSinglePathWithPredicate()
        {
            _expectedActionCounter = 1;
            _patient.Name.Add(new HumanName().WithGiven("Sjimmie").AndFamily("Visser"));
            _sut.VisitByPath(
                _patient,
                ob =>
                {
                    _actualActionCounter++;
                    Assert.IsType<HumanName>(ob);
                    Assert.Equal("Sjimmie", (ob as HumanName).GivenElement.First().ToString());
                },
                "name[given=Sjimmie]");
        }
    }
}