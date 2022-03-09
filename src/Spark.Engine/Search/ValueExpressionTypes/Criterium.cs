// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Search.ValueExpressionTypes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Rest;
    using Support;

    public class Criterium : Expression, ICloneable
    {
        public const string MISSINGMODIF = "missing";
        public const string MISSINGTRUE = "true";
        public const string MISSINGFALSE = "false";
        public const string NOT_MODIFIER = "not";

        //CK: Order of these mappings is important for string matching. From more specific to less specific.
        private static readonly Dictionary<string, Operator> _operatorMapping = new()
        {
            {"ne", Operator.NOT_EQUAL},
            {"ge", Operator.GTE},
            {"le", Operator.LTE},
            {"gt", Operator.GT},
            {"lt", Operator.LT},
            {"sa", Operator.STARTS_AFTER},
            {"eb", Operator.ENDS_BEFORE},
            {"ap", Operator.APPROX},
            {"eq", Operator.EQ},

            // This operator is not allowed on the REST interface: IN(a,b,c) should be formatted as =a,b,c. It is added to allow reporting on criteria.
            
            {"IN", Operator.IN},
            {"", Operator.EQ}
            //CK: Old DSTU1 mapping, will be obsolete in the near future.
            //, new Tuple<string, Operator>( ">=", Operator.GTE)
            //, new Tuple<string, Operator>( "<=", Operator.LTE)
            //, new Tuple<string, Operator>( ">", Operator.GT)
            //, new Tuple<string, Operator>( "<", Operator.LT)
            //, new Tuple<string, Operator>( "~", Operator.APPROX)
        };

        private List<ModelInfo.SearchParamDefinition> _searchParameters;

        public string ParamName { get; set; }

        public Operator Operator { get; set; } = Operator.EQ;

        public string Modifier { get; set; }

        public Expression Operand { get; set; }

        //CK: TODO: This should be SearchParameter, but that does not support Composite parameters very well.
        public List<ModelInfo.SearchParamDefinition> SearchParameters =>
            _searchParameters ??= new List<ModelInfo.SearchParamDefinition>();

        object ICloneable.Clone() => Clone();
        
        public static Criterium Parse(string resourceType, string key, string value)
        {
            if (string.IsNullOrEmpty(key)) throw Error.ArgumentNull("key");
            if (string.IsNullOrEmpty(value)) throw Error.ArgumentNull("value");

            // Split chained parts (if any) into name + modifier tuples
            var chainPath = key.Split(new char[] { SearchParams.SEARCH_CHAINSEPARATOR }, StringSplitOptions.RemoveEmptyEntries)
                .Select(PathToKeyModifTuple);

            if (!chainPath.Any()) throw Error.Argument("key", "Supplied an empty search parameter name or chain");

            return FromPathTuples(chainPath, value, resourceType);
        }
        
        public override string ToString()
        {
            var result = ParamName;

            // Turn ISNULL and NOTNULL operators into the :missing modifier
            if (Operator is Operator.ISNULL or Operator.NOTNULL)
            {
                result += SearchParams.SEARCH_MODIFIERSEPARATOR + MISSINGMODIF;
            }
            else if (!string.IsNullOrEmpty(Modifier))
            {
                result += SearchParams.SEARCH_MODIFIERSEPARATOR + Modifier;
            }

            if (Operator == Operator.CHAIN)
            {
                return Operand is Criterium
                    ? result + SearchParams.SEARCH_CHAINSEPARATOR + Operand
                    : result
                      + SearchParams.SEARCH_CHAINSEPARATOR
                      + " ** INVALID CHAIN OPERATION ** Chain operation must have a Criterium as operand";
            }

            return result + "=" + BuildValue();
        }

        private static Tuple<string, string> PathToKeyModifTuple(string pathPart)
        {
            var pair = pathPart.Split(SearchParams.SEARCH_MODIFIERSEPARATOR);

            var name = pair[0];
            var modifier = pair.Length == 2 ? pair[1] : null;

            return Tuple.Create(name, modifier);
        }

        private static Criterium FromPathTuples(IEnumerable<Tuple<string, string>> path, string value, string resourceType = null)
        {
            var first = path.First();
            var name = first.Item1;
            var modifier = first.Item2;
            var type = FindComparator(value).Item1;
            Expression operand = null;

            // If this is a chained search, unfold the chain first
            if (path.Count() > 1)
            {
                type = Operator.CHAIN;
                operand = FromPathTuples(path.Skip(1), value, resourceType);
            }

            // :missing modifier is actually not a real modifier and is turned into
            // either a ISNULL or NOTNULL operator
            else if (modifier == MISSINGMODIF)
            {
                modifier = null;

                type = value switch
                {
                    MISSINGTRUE => Operator.ISNULL,
                    MISSINGFALSE => Operator.NOTNULL,
                    _ => throw Error.Argument(
                        "value",
                        "For the :missing modifier, only values 'true' and 'false' are allowed")
                };
            }
            // else see if the value starts with a comparator
            else
            {
                // If this an ordered parameter type, then we accept a comparator prefix: https://www.hl7.org/fhir/stu3/search.html#prefix
                if (ModelInfo.SearchParameters.CanHaveOperatorPrefix(resourceType, name))
                {
                    var compVal = FindComparator(value);
                    type = compVal.Item1;
                    value = compVal.Item2;
                }

                if (value == null)
                {
                    throw new FormatException("Value is empty");
                }

                // Parse the value. If there's > 1, we are using the IN operator, unless
                // the input already specifies another comparison, which would be illegal
                var values = ChoiceValue.Parse(value);

                if (values.Choices.Length > 1)
                {
                    if (type != Operator.EQ)
                    {
                        throw new InvalidOperationException(
                            "Multiple values cannot be used in combination with a comparison operator");
                    }

                    type = Operator.IN;
                    operand = values;
                }
                else
                {
                    // Not really a multi value, just a single ValueExpression
                    operand = values.Choices[0];
                }
            }

            // Construct the new criterium based on the parsed values
            return new Criterium {ParamName = name, Operator = type, Modifier = modifier, Operand = operand};
        }

        //private static Operator GetOperator(string value)
        //{
        //    if (_operatorMapping.TryGetValue(value[..2], out var opCode))
        //    {
        //        return opCode;
        //    }

        //    if (_operatorMapping.TryGetValue(value[..1], out opCode))
        //    {
        //        return opCode;
        //    }

        //    return Operator.EQ;
        //}

        private string BuildValue()
        {
            switch (Operator)
            {
                // Turn ISNULL and NOTNULL operators into either true/or false to match the :missing modifier
                case Operator.ISNULL:
                    return "true";
                case Operator.NOTNULL:
                    return "false";
            }

            if (Operand == null)
            {
                throw new InvalidOperationException("Criterium does not have an operand");
            }

            if (Operand is not ValueExpression)
            {
                throw new FormatException("Expected a ValueExpression as operand");
            }

            var value = Operand.ToString();

            return Operator == Operator.EQ
                ? value
                : _operatorMapping.FirstOrDefault(t => t.Value == Operator).Key + value;
        }

        private static Tuple<Operator, string> FindComparator(string value)
        {
            var (item1, item2) = _operatorMapping.FirstOrDefault(t => value.StartsWith(t.Key));

            return Tuple.Create(item2, value[item1.Length..]);
        }

        public Criterium Clone()
        {
            var result = new Criterium
            {
                Modifier = Modifier,
                Operand = Operand is Criterium ? (Operand as Criterium).Clone() : Operand,
                Operator = Operator,
                ParamName = ParamName
            };

            return result;
        }
    }
}
