// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Utility;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Hl7.Fhir.Introspection;

internal static class FhirPathUtil
{
    internal static string ConvertToXPathExpression(string fhirPathExpression)
    {
        const string prefix = "f:";
        const string separator = "/";

        var elements = fhirPathExpression.Split('.');
        var xPathExpression = string.Empty;
        foreach (var element in elements)
        {
            if (string.IsNullOrEmpty(xPathExpression))
            {
                xPathExpression = $"{prefix}{element}";
            }
            else
            {
                xPathExpression += $"{separator}{prefix}{element}";
            }
        }

        return xPathExpression;
    }

    internal static string ResolveToFhirPathExpression(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type resourceType,
        string expression)
    {
        var elements = expression.Split('.');
        var length = elements.Length;
        var fhirPathExpression = string.Empty;
        var currentType = resourceType;
        for (var i = 0; length > i; i++)
        {
            var (element, indexer) = GetElementSeparatedFromIndexer(elements[i]);
            var resolvedElement = ResolveElement(currentType, element);

            fhirPathExpression += $"{resolvedElement.Item2}{indexer}.";

            currentType = resolvedElement.Item1;
        }

        return fhirPathExpression.Length == 0
            ? fhirPathExpression
            : $"{resourceType.Name}.{fhirPathExpression.TrimEnd('.')}";
    }

    private static (Type?, string) ResolveElement(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type? root,
        string element)
    {
        var pi = root?.GetProperty(element);
        if (pi == null)
        {
            return (null, element);
        }

        var fhirElementName = element;
        var fhirElement = pi.GetCustomAttribute<FhirElementAttribute>();
        if (fhirElement != null)
        {
            fhirElementName = fhirElement.Name;
        }

        var elementType = pi.PropertyType.IsGenericType
            ? pi.PropertyType.GetGenericArguments().FirstOrDefault()
            : pi.PropertyType.UnderlyingSystemType;

        return (elementType, fhirElementName);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] private static (string, string) GetElementSeparatedFromIndexer(string element)
    {
        var index = element.LastIndexOf("[", StringComparison.OrdinalIgnoreCase);
        return index > -1 ? (element[..index], element[index..]) : (element, string.Empty);
    }
}
