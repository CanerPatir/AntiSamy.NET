using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AntiSamy.Model
{
    public class DocumentAttribute : ICloneable
    {
        public string Name { get; }
        public string OnInvalid { get; internal set; }
        public string Description { get; internal set; }

        public List<string> AllowedValues { get; } = new List<string>();

        public List<string> AllowedRegExps { get; } = new List<string>();


        public DocumentAttribute(string name, List<string> allowedRegexps, List<string> allowedValues, string onInvalidStr, string description)
        {
            this.Name = name;
            this.AllowedRegExps = allowedRegexps;
            this.AllowedValues = allowedValues;
            this.OnInvalid = onInvalidStr;
            this.Description = description;
        }

        public bool MatchesAllowedExpression(string value)
        {
            string input = value.ToLower();
            foreach (string patternStr in AllowedRegExps)
            {
                if (patternStr == null)
                {
                    continue;
                }
                var pattern = new Regex(patternStr);
                if (pattern.Matches(input).Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public DocumentAttribute Mutate(string onInvalid, string description)
        {
            return new DocumentAttribute(Name,
                AllowedRegExps.ToList(),
                AllowedValues.ToList(),
                !string.IsNullOrEmpty(onInvalid) ? onInvalid : OnInvalid,
                !string.IsNullOrEmpty(description) ? description : Description);
        }

        public string MatcherRegEx(bool hasNext)
        {
            // <p (id=#([0-9.*{6})|sdf).*>

            var regExp = new StringBuilder();
            regExp.Append(Name)
                  .Append(Consts.ANY_NORMAL_WHITESPACES)
                  .Append("=")
                  .Append(Consts.ANY_NORMAL_WHITESPACES)
                  .Append("\"")
                  .Append(Consts.OPEN_ATTRIBUTE);

            bool hasRegExps = AllowedRegExps.Any();

            if (AllowedRegExps.Count() + AllowedValues.Count() > 0)
            {
                foreach (string allowedValue in AllowedValues)
                {
                    regExp.Append(DocumentTag.EscapeRegularExpressionCharacters(allowedValue));

                    if (AllowedValues.Last() != allowedValue || hasRegExps)
                    {
                        regExp.Append(Consts.ATTRIBUTE_DIVIDER);
                    }
                }

                foreach (string allowedRegExp in AllowedRegExps)
                {
                    regExp.Append(allowedRegExp);
                    if (AllowedRegExps.Last() != allowedRegExp)
                    {
                        regExp.Append(Consts.ATTRIBUTE_DIVIDER);
                    }
                }

                if (this.AllowedRegExps.Count() + this.AllowedValues.Count() > 0)
                {
                    regExp.Append(Consts.CLOSE_ATTRIBUTE);
                }

                regExp.Append("\"" + Consts.ANY_NORMAL_WHITESPACES);

                if (hasNext)
                {
                    regExp.Append(Consts.ATTRIBUTE_DIVIDER);
                }
            }
            return regExp.ToString();

        }

        public object Clone() => new DocumentAttribute(Name, AllowedRegExps, AllowedValues, OnInvalid, Description);
    }
}
