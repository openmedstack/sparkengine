// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Hl7.Fhir.Model;

    public class ResourceVisitor
    {
        /// <summary>
        ///     Matches
        ///     value       => head     | predicate     | tail
        ///     a           => "a"      | ""            | ""
        ///     a.b.c       => "a"      | ""            | "b.c"
        ///     a(x=y).b.c  => "a"      | "x=y"         | "b.c"
        ///     See also ResourceVisitorTests.
        /// </summary>
        private readonly Regex _headTailRegex = new(
            @"(?([^\.]*\[.*\])(?<head>[^\[]*)\[(?<predicate>.*)\](\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");

        private readonly Regex _predicateRegex = new(@"(?<propname>[^=]*)=(?<filterValue>.*)");

        private readonly FhirPropertyIndex _propIndex;

        public ResourceVisitor(FhirPropertyIndex propIndex) => _propIndex = propIndex;
        
        /// <summary>
        ///     Walk through an object, following the specified path of properties.
        ///     The path should NOT include the name of the resource itself (e.g. "Patient.birthdate" is wrong, "birthdate" is
        ///     right).
        /// </summary>
        /// <param name="fhirObject"></param>
        /// <param name="action"></param>
        /// <param name="path"></param>
        /// <param name="predicate"></param>
        public void VisitByPath(object fhirObject, Action<object> action, string path, string predicate = null)
        {
            if (fhirObject == null)
            {
                return;
            }

            //List of items, visit each of them.
            if (TestIfGenericList(fhirObject.GetType()))
            {
                VisitByPath(fhirObject as IEnumerable<Base>, action, path, predicate);
            }
            //Single item, visit it if it adheres to the predicate (if any)
            else if (string.IsNullOrEmpty(predicate) || PredicateIsTrue(predicate, fhirObject))
            {
                //Path has ended, we arrived at the object that needs action.
                if (string.IsNullOrEmpty(path))
                {
                    action(fhirObject);
                }
                //See what else is in the path and recursively visit that.
                else
                {
                    var hpt = HeadPredicateAndTail(path);
                    var head = hpt.Item1.TrimStart('@');
                    var headPredicate = hpt.Item2;
                    var tail = hpt.Item3;

                    //Path was not empty, so there should be a head. No need for an extra null-check.
                    var pm = _propIndex.FindPropertyInfo(fhirObject.GetType(), head);

                    //Path might denote an unknown property.
                    var headValue = pm?.PropInfo.GetValue(fhirObject);

                    if (headValue != null)
                    {
                        VisitByPath(headValue, action, tail, headPredicate);
                    }
                }
            }
        }

        private void VisitByPath(IEnumerable<object> fhirObjects, Action<object> action, string path, string predicate)
        {
            foreach (var fhirObject in fhirObjects)
            {
                VisitByPath(fhirObject, action, path, predicate);
            }
        }

        private Tuple<string, string, string> HeadPredicateAndTail(string path)
        {
            var match = _headTailRegex.Match(path);
            var head = match.Groups["head"].Value;
            var predicate = match.Groups["predicate"].Value;
            var tail = match.Groups["tail"].Value;

            return new Tuple<string, string, string>(head, predicate, tail);
        }

        private bool PredicateIsTrue(string predicate, object fhirObject)
        {
            var match = _predicateRegex.Match(predicate);
            if (!match.Success)
            {
                return false;
            }

            var propertyName = match.Groups["propname"].Value;
            var filterValue = match.Groups["filterValue"].Value.Trim('\'');

            var result = false;

            //Handle the predicate by (again recursively) visiting from here.
            VisitByPath(
                fhirObject,
                el =>
                {
                    var actualValue = TestIfCodedEnum(el.GetType())
                        ? el.GetType().GetProperty("Value").GetValue(el).ToString()
                        : el.ToString();

                    result = filterValue.Equals(actualValue, StringComparison.InvariantCultureIgnoreCase);
                },
                propertyName //No support for nested predicates.
            );

            return result;
        }

        /// <summary>
        ///     Test if a type derives from IList of T, for any T.
        /// </summary>
        private bool TestIfGenericList(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var interfaceTest =
                new Predicate<Type>(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

            return interfaceTest(type) || type.GetInterfaces().Any(i => interfaceTest(i));
        }

        //TODO: Do not repeat this code. It is also in ElementIndexer (and in ElementQuery, but that will be retired some day soon).
        private bool TestIfCodedEnum(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var codedEnum = type.GenericTypeArguments.FirstOrDefault()?.IsEnum;
            return codedEnum.HasValue && codedEnum.Value;
        }
    }
}