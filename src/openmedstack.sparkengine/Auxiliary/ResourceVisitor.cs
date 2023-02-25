// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Auxiliary;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Hl7.Fhir.Model;

public static class ResourceVisitor
{
    public static void VisitByType(object item, Visitor action, params Type[] filter)
    {
        // This is a filter that returns true if the property in pInfo is a subtype
        // of one of the types given in the filter. Because of this, scan() returns
        // all Elements in item that are of the types in filter, or subclasses.
        void Visitor(Element elem, string path)
        {
            foreach (var t in filter)
            {
                var type = elem.GetType();
                if (t.IsAssignableFrom(type))
                {
                    action(elem, path);
                }
            }
        }

        Scan(item, null, Visitor);
    }

    private static bool PropertyFilter(
        MemberInfo mem,
        object? arg)
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        static Type GetPropertyType(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType;
        }
        // We prefilter on properties, so this cast is always valid
        var prop = (PropertyInfo)mem;

        // Return true if the property is either an Element or an IEnumerable<Element>.
        var isElementProperty = typeof(Element).IsAssignableFrom(GetPropertyType(prop));
        var collectionInterface = GetPropertyType(prop).GetInterface("IEnumerable`1");
        var isElementCollection = false;
        var hasIndexParameters = prop.GetIndexParameters().Length > 0;

        if (collectionInterface == null)
        {
            return (isElementProperty || isElementCollection) && hasIndexParameters == false;
        }

        var firstGenericArg = collectionInterface.GetGenericArguments()[0];
        isElementCollection = typeof(Element).IsAssignableFrom(firstGenericArg);

        return (isElementProperty || isElementCollection) && hasIndexParameters == false;
    }

    private static string JoinPath(string old, string part) => !string.IsNullOrEmpty(old) ? old + "." + part : part;

    private static void
        Scan<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
            T? item,
            string? path,
            Visitor visitor)
        where T : class
    {
        if (item == null)
        {
            return;
        }

        path ??= string.Empty;

        // Scan the object 'item' and find all properties of type Element of IEnumerable<Element>
        var result = typeof(T).FindMembers(
            MemberTypes.Property,
            BindingFlags.Instance | BindingFlags.Public,
            PropertyFilter,
            null);

        // Do a depth-first traversal of the properties and their contents
        foreach (var property in result.OfType<PropertyInfo>())
        {
            // If this member is an IEnumerable<Element>, go inside and recurse
            if (property.PropertyType.GetInterface("IEnumerable`1") != null)
            {
                // Since we filter for Properties of Element or IEnumerable<Element>
                // this cast should always work

                if (property.GetValue(item, null) is IEnumerable<Element> list)
                {
                    var index = 0;
                    foreach (var element in list)
                    {
                        var propertyPath = JoinPath(path, property.Name + "[" + index + "]");

                        visitor(element, propertyPath);
                        Scan(element, propertyPath, visitor);
                    }
                }
            }

            // If this member is an Element, go inside and recurse
            else
            {
                var propertyPath = JoinPath(path, property.Name);

                var propValue = (Element?)property.GetValue(item);

                // Look into the property to find nested elements
                if (propValue != null)
                {
                    visitor(propValue, propertyPath);
                    Scan(propValue, propertyPath, visitor);
                }
            }
        }
    }
}
