using System.Collections.Generic;

namespace AntiSamy.Model
{
    public class CssProperty
    {
        public CssProperty(string name, IEnumerable<string> allowedRegexps, IEnumerable<string> allowedLiterals, IEnumerable<string> shortHandRefs, string description, string onInvalid)
        {
            Description = description;
            Name = name;
            OnInvalid = onInvalid;
            AllowedRegExps = allowedRegexps ?? new List<string>();
            AllowedLiterals = allowedLiterals ?? new List<string>();
            ShorthandRefs = shortHandRefs ?? new List<string>();
        }

        public string Description { get; }

        public string Name { get; }

        public string OnInvalid { get; }

        public IEnumerable<string> AllowedLiterals { get; }

        public IEnumerable<string> AllowedRegExps { get; }

        public IEnumerable<string> ShorthandRefs { get; }
    }
}
