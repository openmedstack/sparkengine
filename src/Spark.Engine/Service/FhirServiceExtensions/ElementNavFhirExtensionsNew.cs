﻿// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Hl7.Fhir.ElementModel;
    using Hl7.Fhir.FhirPath;
    using Hl7.Fhir.Model;
    using Hl7.FhirPath;

    /// <summary>
    ///     FIXME: the aim of this extension is to fix a single issue in the original ElementNavFhirExtensions.Select.
    ///     The issue happens when it's used together with some expression selecting property of type Date, but the
    ///     underlying value is of PrimitiveType, say, "2020-12-10".
    ///     Cause of this: it doesn't support Hl7.Fhir.ElementModel.Types.Date type (only Hl7.Fhir.ElementModel.Types.DateTime
    ///     is originally supported).
    ///     Test failing: X012_Goal.
    ///     TODO: create PR in the original repo and fix it.
    /// </summary>
    internal static class ElementNavFhirExtensionsNew
    {
        public static IEnumerable<Base> SelectNew(this Base input, string expression, FhirEvaluationContext ctx = null)
        {
            var inputNav = input.ToTypedElement();
            var result = inputNav.Select(expression, ctx ?? FhirEvaluationContext.CreateDefault());
            return ToFhirValues(result);
        }

        public static IEnumerable<Base> ToFhirValues(this IEnumerable<ITypedElement> results)
        {
            return results.Select(
                r =>
                {
                    if (r == null)
                    {
                        return null;
                    }

                    var fhirValueProvider = r.Annotation<IFhirValueProvider>();
                    if (fhirValueProvider != null)
                    {
                        return fhirValueProvider.FhirValue;
                    }

                    var obj = r.Value;
                    return obj switch
                    {
                        bool flag => new FhirBoolean(flag),
                        long num => new Integer((int) num),
                        decimal num => new FhirDecimal(num),
                        string s => new FhirString(s),
                        Hl7.Fhir.ElementModel.Types.Date date => new Date(date.ToString()),
                        Hl7.Fhir.ElementModel.Types.DateTime dateTime => new FhirDateTime(
                            dateTime.ToDateTimeOffset(TimeSpan.Zero).ToUniversalTime()),
                        _ => r.Value as Base
                    };
                });
        }
    }
}