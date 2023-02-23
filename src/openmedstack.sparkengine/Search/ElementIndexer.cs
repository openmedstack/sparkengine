// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Search;

using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Extensions;
using Hl7.Fhir.Model;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using SparkEngine.Model;
using ValueExpressionTypes;
using Code = Hl7.Fhir.Model.Code;
using Date = Hl7.Fhir.Model.Date;
using Expression = ValueExpressionTypes.Expression;
using Integer = Hl7.Fhir.Model.Integer;
using Quantity = Hl7.Fhir.Model.Quantity;
using Time = Hl7.Fhir.Model.Time;

//This class is not static because it needs a IFhirModel to do some of the indexing (especially enums).
public class ElementIndexer
{
    private readonly IFhirModel _fhirModel;
    private readonly ILogger<ElementIndexer> _logger;
    private readonly IReferenceNormalizationService? _referenceNormalizationService;

    public ElementIndexer(
        IFhirModel fhirModel,
        ILogger<ElementIndexer> logger,
        IReferenceNormalizationService? referenceNormalizationService = null)
    {
        _fhirModel = fhirModel;
        _logger = logger;
        _referenceNormalizationService = referenceNormalizationService;
    }

    private static List<Expression> ListOf(params Expression?[] args)
    {
        if (!args.Any())
        {
            return new List<Expression>();
        }

        var result = new List<Expression>();
        result.AddRange(args.Where(x => x != null).Select(x => x!));
        return result;
    }

    /// <summary>
    ///     Maps element to a list of Expression.
    /// </summary>
    /// <param name="element"></param>
    /// <returns>List of Expression, empty List if no mapping was possible.</returns>
    public List<Expression> Map(Element element)
    {
        var result = new List<Expression>();
        try
        {
            // TODO: How to handle composite SearchParameter type
            //if (element is Sequence.VariantComponent) return result;
            List<Expression> expressions = ToExpressions((dynamic)element);
            if (expressions != null)
            {
                result.AddRange(expressions);
            }
        }
        catch (RuntimeBinderException)
        {
            _logger.LogError("ElementIndexer.Map: Mapping of type {type}", element.GetType().Name);
        }

        return result;
    }

    private List<Expression> ToExpressions(Age element) => ToExpressions(element as Quantity);

    private static List<Expression> ToExpressions(Location.PositionComponent element)
    {
        if (element.Latitude == null || element.Longitude == null)
        {
            return new List<Expression>();
        }

        var position = new List<IndexValue>
        {
            new("latitude", new NumberValue(element.Latitude!.Value)),
            new("longitude", new NumberValue(element.Longitude!.Value))
        };

        return ListOf(new CompositeValue(position));
    }

    private static List<Expression> ToExpressions(Base64Binary element) =>
        element.Value.Length == 0
            ? new()
            : ToExpressions(new FhirString(element.ToString()));

    private static List<Expression> ToExpressions(Attachment element) => ToExpressions(element.UrlElement);

    private static List<Expression> ToExpressions(Timing element)
    {
        if (element.Repeat?.Bounds == null)
        {
            return new();
        }

        // TODO: Should I handle Duration?
        return element.Repeat.Bounds is Period p ? ToExpressions(p) : new();
    }

    private List<Expression> ToExpressions(Extension element) =>
        (List<Expression>)ToExpressions((dynamic)element.Value);

    private static List<Expression> ToExpressions(Markdown element) =>
        string.IsNullOrWhiteSpace(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(Id element) =>
        string.IsNullOrWhiteSpace(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(Oid element) =>
        string.IsNullOrWhiteSpace(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(Integer element) =>
        !element.Value.HasValue ? new() : ListOf(new NumberValue(element.Value.Value));

    private static List<Expression> ToExpressions(UnsignedInt element) =>
        !element.Value.HasValue ? new() : ListOf(new NumberValue(element.Value.Value));

    private static List<Expression> ToExpressions(PositiveInt element) =>
        !element.Value.HasValue ? new() : ListOf(new NumberValue(element.Value.Value));

    private static List<Expression> ToExpressions(Instant element)
    {
        if (!element.Value.HasValue)
        {
            return new();
        }

        var fdt = new FhirDateTime(element.Value.Value);
        return ToExpressions(fdt);
    }

    private static List<Expression> ToExpressions(Time element) =>
        string.Empty.Equals(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(FhirUrl element) =>
        string.Empty.Equals(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(FhirUri element) =>
        string.Empty.Equals(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(Canonical element) =>
        string.Empty.Equals(element.Value) ? new() : ListOf(new StringValue(element.Value));

    private static List<Expression> ToExpressions(Date element)
    {
        if (string.Empty.Equals(element.Value))
        {
            return new();
        }

        var fdt = new FhirDateTime(element.Value);
        return ToExpressions(fdt);
    }

    private static List<Expression> ToExpressions(FhirDecimal element) =>
        !element.Value.HasValue ? new() : ListOf(new NumberValue(element.Value.Value));

    /// <summary>
    ///     { start : lowerbound-of-fhirdatetime, end : upperbound-of-fhirdatetime }
    ///     <seealso cref="ToExpressions(Period)" />, with lower and upper bounds of FhirDateTime as bounds of the Period.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static List<Expression> ToExpressions(FhirDateTime element)
    {
        var bounds = new List<IndexValue>
        {
            new("start", new DateTimeValue(element.LowerBound())),
            new("end", new DateTimeValue(element.UpperBound()))
        };


        return ListOf(new CompositeValue(bounds));
    }

    /// <summary>
    ///     { start : lowerbound-of-period-start, end : upperbound-of-period-end }
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static List<Expression> ToExpressions(Period element)
    {
        if (element.StartElement == null && element.EndElement == null)
        {
            return new();
        }

        var bounds = new List<IndexValue>();
        if (element.StartElement != null)
        {
            bounds.Add(new IndexValue("start", new DateTimeValue(element.StartElement.LowerBound())));
        }

        if (element.EndElement != null)
        {
            bounds.Add(new IndexValue("end", new DateTimeValue(element.EndElement.UpperBound())));
        }

        return ListOf(new CompositeValue(bounds));
    }

    private static List<Expression> ToExpressions(Code code)
    {
        return new List<Expression> { new StringValue(code.Value) };
    }

    /// <summary>
    ///     { system : system1, code: code1, text: display1 },
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static List<Expression> ToExpressions(Coding element)
    {
        var values = new List<IndexValue>();
        if (element.Code != null)
        {
            values.Add(new IndexValue("code", new StringValue(element.Code)));
        }

        if (element.System != null)
        {
            values.Add(new IndexValue("system", new StringValue(element.System)));
        }

        if (element.Display != null)
        {
            values.Add(new IndexValue("text", new StringValue(element.Display)));
        }

        return ListOf(new CompositeValue(values));
    }

    /// <summary>
    ///     { code : identifier-value, system : identifier-system, text : identifier-type }
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static List<Expression> ToExpressions(Identifier element)
    {
        var values = new List<IndexValue>();
        if (element.Value != null)
        {
            values.Add(new IndexValue("code", new StringValue(element.Value)));
        }

        if (element.System != null)
        {
            values.Add(new IndexValue("system", new StringValue(element.System)));
        }

        if (element.Type != null)
        {
            values.Add(new IndexValue("text", new StringValue(element.Type.Text)));
        }

        return ListOf(new CompositeValue(values));
    }

    /// <summary>
    ///     [
    ///     { system : system1, code: code1, text: display1 },
    ///     { system : system2, code: code2, text: display2 },
    ///     text : codeableconcept-text
    ///     ]
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static List<Expression> ToExpressions(CodeableConcept element)
    {
        var result = new List<Expression>();
        if (element.Coding != null && element.Coding.Any())
        {
            result.AddRange(element.Coding.SelectMany(ToExpressions));
        }

        if (element.Text != null)
        {
            result.Add(new IndexValue("text", new StringValue(element.Text)));
        }

        return result;
    }

    /// <summary>
    ///     { code : contactpoint-value, system : contactpoint-use }
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private List<Expression> ToExpressions(ContactPoint element)
    {
        var values = new List<IndexValue>();
        if (element.Value != null)
        {
            values.Add(new IndexValue("code", Map(element.ValueElement)));
        }

        if (element.System != null)
        {
            values.Add(new IndexValue("system", Map(element.SystemElement)));
        }

        if (element.Use != null)
        {
            values.Add(new IndexValue("use", Map(element.UseElement)));
        }

        return ListOf(new CompositeValue(values));
    }

    /// <summary>
    ///     { code : true/false }, system is absent
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static List<Expression> ToExpressions(FhirBoolean element)
    {
        if (element.Value == null)
        {
            return new();
        }

        var values = new List<IndexValue>
        {
            new("code", element.Value.Value ? new StringValue("true") : new StringValue("false"))
        };

        return ListOf(new CompositeValue(values));

        //TODO: Include implied system: http://hl7.org/fhir/special-values ?
    }

    private List<Expression>? ToExpressions(ResourceReference? element)
    {
        if (element == null)
        {
            return null;
        }

        if (element.Url != null)
        {
            Expression? value = null;
            var uri = element.Url;
            if (uri.IsAbsoluteUri)
            {
                //This is a fully specified url, either internal or external. Don't change it.
                var stringValue = new StringValue(uri.ToString());

                // normalize reference value to be able to use normalized criteria for search.
                // https://github.com/FirelyTeam/spark/issues/35 
                value = _referenceNormalizationService != null
                    ? _referenceNormalizationService.GetNormalizedReferenceValue(stringValue, null)
                    : stringValue;
            }
            else
            {
                //This is a relative url, so it is meant to point to something internal to our server.
                value = new StringValue(uri.ToString());
                //TODO: expand to absolute url with Localhost?
            }

            return ListOf(value);
        }

        if (element.Identifier != null)
        {
            return ToExpressions(element.Identifier);
        }

        return null;
    }

    /// <summary>
    ///     Returns list of all Address elements
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private List<Expression> ToExpressions(Address element)
    {
        var values = new List<Expression>();
        if (element.City != null) values.Add(new StringValue(element.City));
        if (element.Country != null) values.Add(new StringValue(element.Country));
        if (element.State != null) values.Add(new StringValue(element.State));
        if (element.Text != null) values.Add(new StringValue(element.Text));
        if (element.Use.HasValue) values.Add(new StringValue(_fhirModel.GetLiteralForEnum(element.Use.Value)));
        if (element.PostalCode != null) values.Add(new StringValue(element.PostalCode));

        values.AddRange(element.Line?.Select(line => new StringValue(line)) ?? Array.Empty<StringValue>());

        return values;
    }

    /// <summary>
    ///     Returns list of Given and Family parts of the name
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private List<Expression> ToExpressions(HumanName element)
    {
        var values = new List<Expression>();
        values.AddRange(ToExpressions(element.GivenElement));
        if (element.FamilyElement != null)
        {
            values.AddRange(ToExpressions(element.FamilyElement));
            if (element.PrefixElement is { Count: > 0 })
            {
                values.AddRange(ToExpressions(element.PrefixElement));
            }

            if (element.SuffixElement is { Count: > 0 })
            {
                values.AddRange(ToExpressions(element.SuffixElement));
            }

            if (element.TextElement != null)
            {
                values.AddRange(ToExpressions(element.TextElement));
            }
        }

        return values;
    }

    private List<Expression> ToExpressions(Quantity element)
    {
        try
        {
            return ListOf(element.ToExpression());
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(
                $"unknown element: Quantity: {element.Code} {element.Unit} {element.Value}",
                ex.Message);

            return new();
        }
    }

    private static List<Expression> ToExpressions(FhirString element) => ListOf(new StringValue(element.Value));

    private List<Expression> ToExpressions(IEnumerable<Element> elements) => elements.SelectMany(Map).ToList();

    private List<Expression> ToExpressions<T>(Code<T> element)
        where T : struct, Enum
    {
        if (element.Value is Enum e)
        {
            var values = new List<IndexValue>
            {
                new("code", new StringValue(_fhirModel.GetLiteralForEnum(e)))
            };
            return ListOf(new CompositeValue(values));
        }

        return new();
    }
}