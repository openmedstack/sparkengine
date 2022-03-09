// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Test.Extensions
{
    using System;
    using Engine.Extensions;
    using Hl7.Fhir.Model;
    using Xunit;

    public class OperationOutcomeExtensionsTests
    {
        [Fact]
        public void Three_Level_InnerErrors_Test()
        {
            OperationOutcome outcome;

            try
            {
                try
                {
                    try
                    {
                        throw new Exception("Third error level");
                    }
                    catch (Exception e3)
                    {
                        throw new Exception("Second error level", e3);
                    }
                }
                catch (Exception e2)
                {
                    throw new Exception("First error level", e2);
                }
            }
            catch (Exception e1)
            {
                outcome = new OperationOutcome().AddAllInnerErrors(e1);
            }

            Assert.True(
                outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: First error level")) == 0,
                "First error level should be at index 0");
            Assert.True(
                outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Second error level")) == 1,
                "Second error level should be at index 1");
            Assert.True(
                outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Third error level")) == 2,
                "Third error level should be at index 2");
        }

        [Fact]
        public void IssueSeverity_Is_Information_When_HttpStatusCode_Is_Continue_Test()
        {
            Assert.Equal(
                OperationOutcome.IssueSeverity.Information,
                OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.Continue));
        }

        [Fact]
        public void IssueSeverity_Is_Information_When_HttpStatusCode_Is_Created_Test()
        {
            Assert.Equal(
                OperationOutcome.IssueSeverity.Information,
                OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.Created));
        }

        [Fact]
        public void IssueSeverity_Is_Warning_When_HttpStatusCode_Is_MovedPermanently_Test()
        {
            Assert.Equal(
                OperationOutcome.IssueSeverity.Warning,
                OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.MovedPermanently));
        }

        [Fact]
        public void IssueSeverity_Is_Error_When_HttpStatusCode_Is_BadRequest_Test()
        {
            Assert.Equal(
                OperationOutcome.IssueSeverity.Error,
                OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.BadRequest));
        }

        [Fact]
        public void IssueSeverity_Is_Fatal_When_HttpStatusCode_Is_InternalServerError_Test()
        {
            Assert.Equal(
                OperationOutcome.IssueSeverity.Fatal,
                OperationOutcomeExtensions.IssueSeverityOf(System.Net.HttpStatusCode.InternalServerError));
        }
    }
}