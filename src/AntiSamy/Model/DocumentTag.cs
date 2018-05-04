using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntiSamy.Model
{
    public class DocumentTag
    {
        private static readonly string OPEN_TAG_ATTRIBUTES = Consts.ANY_NORMAL_WHITESPACES + Consts.OPEN_ATTRIBUTE;
        private static readonly string CLOSE_TAG_ATTRIBUTES = ")*";
        private static readonly string REGEXP_CHARACTERS = "\\(){}.*?$^-+";

        private readonly Dictionary<string, DocumentAttribute> _allowedAttributes = new Dictionary<string, DocumentAttribute>();

        public DocumentTag(string name, string action)
        {
            Name = name;
            Action = action;
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, DocumentAttribute> AllowedAttributes => _allowedAttributes;

        public string Action { get; }

        public void AddAllowedAttribute(DocumentAttribute attr)
        {
            _allowedAttributes[attr.Name] = attr;
        }

        public bool IsAction(string action) => action.Equals(Action);

        public string GetRegularExpression()
        {
            if (_allowedAttributes.Count == 0)
            {
                return "^<" + Name + ">$";
            }

            var regExp = new StringBuilder("<" + Consts.ANY_NORMAL_WHITESPACES + Name + OPEN_TAG_ATTRIBUTES);

            List<DocumentAttribute> values = _allowedAttributes.Values.OrderBy(a => a.Name).ToList();

            foreach (DocumentAttribute attr in values)
            {
                regExp.Append(attr.MatcherRegEx(values.Last() != attr));
            }

            regExp.Append(CLOSE_TAG_ATTRIBUTES + Consts.ANY_NORMAL_WHITESPACES + ">");

            return regExp.ToString();
        }

        public static string EscapeRegularExpressionCharacters(string allowedValue)
        {
            string toReturn = allowedValue;
            if (toReturn == null)
            {
                return null;
            }

            for (var i = 0; i < REGEXP_CHARACTERS.Length; i++)
            {
                toReturn = toReturn.Replace("\\" + Convert.ToString(REGEXP_CHARACTERS.ElementAt(i)), "\\" + REGEXP_CHARACTERS.ElementAt(i));
            }

            return toReturn;
        }

        public DocumentAttribute GetAttributeByName(string name) => _allowedAttributes.TryGetValue(name, out DocumentAttribute val) ? val : null;
    }
}
