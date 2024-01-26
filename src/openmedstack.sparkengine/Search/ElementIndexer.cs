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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extensions;
using Hl7.Fhir.Model;
using Interfaces;
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

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Expression))]
    private static Expression[] ListOf(params Expression?[] args)
    {
        var result = args.Where(x => x != null).Select(x => x!).ToArray();
        return result;
    }

    /// <summary>
    ///     Maps element to a list of Expression.
    /// </summary>
    /// <param name="element"></param>
    /// <returns>List of Expression, empty List if no mapping was possible.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Element))]
    public Expression[] Map(Element element)
    {
        try
        {
            // TODO: How to handle composite SearchParameter type
            //if (element is Sequence.VariantComponent) return result;
            var expressions = ToExpressions((dynamic)element);
            if (expressions is Expression[])
            {
                return expressions;
            }
        }
        catch (RuntimeBinderException)
        {
            _logger.LogError("ElementIndexer.Map: Mapping of type {type}", element.GetType().Name);
        }

        return Array.Empty<Expression>();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Age))]
    private Expression[] ToExpressions(Age element) => ToExpressions(element as Quantity);

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Location.PositionComponent))]
    private static Expression[] ToExpressions(Location.PositionComponent element)
    {
        if (element.Latitude == null || element.Longitude == null)
        {
            return Array.Empty<Expression>();
        }

        var position = new List<IndexValue>
        {
            new("latitude", new NumberValue(element.Latitude!.Value)),
            new("longitude", new NumberValue(element.Longitude!.Value))
        };

        return ListOf(new CompositeValue(position));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Base64Binary))]
    private static Expression[] ToExpressions(Base64Binary element) =>
        element.Value.Length == 0
            ? Array.Empty<Expression>()
            : ToExpressions(new FhirString(element.ToString()));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Attachment))]
    private static Expression[] ToExpressions(Attachment element) => ToExpressions(element.UrlElement);

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Timing))]
    private static Expression[] ToExpressions(Timing element)
    {
        if (element.Repeat?.Bounds == null)
        {
            return Array.Empty<Expression>();
        }

        // TODO: Should I handle Duration?
        return element.Repeat.Bounds is Period p ? ToExpressions(p) : Array.Empty<Expression>();
    }

#pragma warning disable IL2026 // Using dynamic types might cause types or members to be removed by trimmer.
    private Expression[] ToExpressions(Extension element) =>
        (Expression[])ToExpressions((dynamic)element.Value);
#pragma warning restore IL2026 // Using dynamic types might cause types or members to be removed by trimmer.

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Markdown))]
    private static Expression[] ToExpressions(Markdown element) =>
        string.IsNullOrWhiteSpace(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Id))]
    private static Expression[] ToExpressions(Id element) =>
        string.IsNullOrWhiteSpace(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Oid))]
    private static Expression[] ToExpressions(Oid element) =>
        string.IsNullOrWhiteSpace(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Integer))]
    private static Expression[] ToExpressions(Integer element) =>
        !element.Value.HasValue ? Array.Empty<Expression>() : ListOf(new NumberValue(element.Value.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(UnsignedInt))]
    private static Expression[] ToExpressions(UnsignedInt element) =>
        !element.Value.HasValue ? Array.Empty<Expression>() : ListOf(new NumberValue(element.Value.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(PositiveInt))]
    private static Expression[] ToExpressions(PositiveInt element) =>
        !element.Value.HasValue ? Array.Empty<Expression>() : ListOf(new NumberValue(element.Value.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Instant))]
    private static Expression[] ToExpressions(Instant element)
    {
        if (!element.Value.HasValue)
        {
            return Array.Empty<Expression>();
        }

        var fdt = new FhirDateTime(element.Value.Value);
        return ToExpressions(fdt);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Time))]
    private static Expression[] ToExpressions(Time element) =>
        string.Empty.Equals(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FhirUrl))]
    private static Expression[] ToExpressions(FhirUrl element) =>
        string.Empty.Equals(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FhirUri))]
    private static Expression[] ToExpressions(FhirUri element) =>
        string.Empty.Equals(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Canonical))]
    private static Expression[] ToExpressions(Canonical element) =>
        string.Empty.Equals(element.Value) ? Array.Empty<Expression>() : ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Date))]
    private static Expression[] ToExpressions(Date element)
    {
        if (string.Empty.Equals(element.Value))
        {
            return Array.Empty<Expression>();
        }

        var fdt = new FhirDateTime(element.Value);
        return ToExpressions(fdt);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FhirDecimal))]
    private static Expression[] ToExpressions(FhirDecimal element) =>
        !element.Value.HasValue ? Array.Empty<Expression>() : ListOf(new NumberValue(element.Value.Value));

    /// <summary>
    ///     { start : lowerbound-of-fhirdatetime, end : upperbound-of-fhirdatetime }
    ///     <seealso cref="ToExpressions(Period)" />, with lower and upper bounds of FhirDateTime as bounds of the Period.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FhirDateTime))]
    private static Expression[] ToExpressions(FhirDateTime element)
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Period))]
    private static Expression[] ToExpressions(Period element)
    {
        if (element.StartElement == null && element.EndElement == null)
        {
            return Array.Empty<Expression>();
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

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Code))]
    private static Expression[] ToExpressions(Code code)
    {
        return [new StringValue(code.Value)];
    }

    /// <summary>
    ///     { system : system1, code: code1, text: display1 },
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Coding))]
    private static Expression[] ToExpressions(Coding element)
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Identifier))]
    private static Expression[] ToExpressions(Identifier element)
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CodeableConcept))]
    private static Expression[] ToExpressions(CodeableConcept element)
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

        return result.ToArray();
    }

    /// <summary>
    ///     { code : contactpoint-value, system : contactpoint-use }
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ContactPoint))]
    private Expression[] ToExpressions(ContactPoint element)
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FhirBoolean))]
    private static Expression[] ToExpressions(FhirBoolean element)
    {
        if (element.Value == null)
        {
            return Array.Empty<Expression>();
        }

        var values = new List<IndexValue>
        {
            new("code", element.Value.Value ? new StringValue("true") : new StringValue("false"))
        };

        return ListOf(new CompositeValue(values));

        //TODO: Include implied system: http://hl7.org/fhir/special-values ?
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ResourceReference))]
    private Expression[]? ToExpressions(ResourceReference? element)
    {
        if (element == null)
        {
            return null;
        }

        if (element.Url != null)
        {
            Expression? value;
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Address))]
    private Expression[] ToExpressions(Address element)
    {
        var values = new List<Expression>();
        if (element.City != null)
        {
            values.Add(new StringValue(element.City));
        }

        if (element.Country != null)
        {
            values.Add(new StringValue(element.Country));
        }

        if (element.State != null)
        {
            values.Add(new StringValue(element.State));
        }

        if (element.Text != null)
        {
            values.Add(new StringValue(element.Text));
        }

        if (element.Use.HasValue)
        {
            values.Add(new StringValue(_fhirModel.GetLiteralForEnum(element.Use.Value)));
        }

        if (element.PostalCode != null)
        {
            values.Add(new StringValue(element.PostalCode));
        }

        values.AddRange(element.Line?.Select(line => new StringValue(line)) ?? Array.Empty<StringValue>());

        return values.ToArray();
    }

    /// <summary>
    ///     Returns list of Given and Family parts of the name
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(HumanName))]
    private Expression[] ToExpressions(HumanName element)
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

        return values.ToArray();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Quantity))]
    private Expression[] ToExpressions(Quantity element)
    {
        try
        {
            return ListOf(element.ToExpression());
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex,
                "unknown element: Quantity: {code} {unit} {value}",
                element.Code,
                element.Unit,
                element.Value);

            return Array.Empty<Expression>();
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FhirString))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(StringValue))]
    private static Expression[] ToExpressions(FhirString element) => ListOf(new StringValue(element.Value));

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Element))]
    private Expression[] ToExpressions(IEnumerable<Element> elements) => elements.SelectMany(Map).ToArray();

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Code<>))]
    private Expression[] ToExpressions<T>(Code<T> element)
        where T : struct, Enum
    {
        switch (element.Value)
        {
            case Enum e:
            {
                var values = new List<IndexValue>
                {
                    new("code", new StringValue(_fhirModel.GetLiteralForEnum(e)))
                };
                return ListOf(new CompositeValue(values));
            }
            default:
                return Array.Empty<Expression>();
        }
    }
}
