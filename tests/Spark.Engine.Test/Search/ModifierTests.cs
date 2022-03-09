// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Search
{
    using Engine.Search.Model;
    using Hl7.Fhir.Model;
    using Xunit;

    public class ModifierTests
    {
        [Fact]
        public void TestActualModifierConstructorWithMissingModifiers()
        {
            var am = new ActualModifier("missing");
            Assert.Equal(Modifier.MISSING, am.Modifier);
            Assert.Equal("missing", am.RawModifier);
            Assert.Null(am.ModifierType);
            Assert.True(am.Missing.Value);
            Assert.Equal("missing=true", am.ToString());

            am = new ActualModifier("missing=false");
            Assert.Equal(Modifier.MISSING, am.Modifier);
            Assert.Equal("missing=false", am.RawModifier);
            Assert.Null(am.ModifierType);
            Assert.False(am.Missing.Value);
            Assert.Equal("missing=false", am.ToString());
        }

        [Fact]
        public void TestActualModifierConstructorWithValidTypeModifier()
        {
            var am = new ActualModifier("Patient");
            Assert.Equal(Modifier.TYPE, am.Modifier);
            Assert.Equal("Patient", am.RawModifier);
            Assert.Equal(typeof(Patient), am.ModifierType);
            Assert.Equal("Patient", am.ToString());
        }

        [Fact]
        public void TestActualModifierConstructorWithInvalidModifier()
        {
            var am = new ActualModifier("blabla");
            Assert.Equal(Modifier.UNKNOWN, am.Modifier);
            Assert.Equal("blabla", am.RawModifier);
            Assert.Null(am.ModifierType);
            Assert.Null(am.ToString());
        }
    }
}