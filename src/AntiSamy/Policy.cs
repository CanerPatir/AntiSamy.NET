using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using AntiSamy.Model;

namespace AntiSamy
{
    public class Policy
    {
        public const string DefaultOninvalid = "removeAttribute";
        public const int DefaultMaxInputSize = 100000;
        private const char RegexpBegin = '^';
        private const char RegexpEnd = '$';
        private readonly bool _fromXml;
        private readonly string _xml;

        private Policy(FileInfo file)
            : this(file.FullName, false)
        {
        }

        private Policy(string xml, bool fromXml)
        {
            _xml = xml ?? throw new ArgumentNullException(nameof(xml));
            _fromXml = fromXml;
        }

        public IReadOnlyDictionary<string, DocumentAttribute> CommonAttributes { get; private set; } = new Dictionary<string, DocumentAttribute>();

        public IReadOnlyDictionary<string, string> CommonRegularExpressions { get; private set; } = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, CssProperty> CssRules { get; private set; } = new Dictionary<string, CssProperty>();

        public IReadOnlyDictionary<string, DocumentTag> TagRules { get; private set; } = new Dictionary<string, DocumentTag>();

        public IReadOnlyDictionary<string, string> Directives { get; private set; } = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, DocumentAttribute> GlobalTagAttributes { get; private set; } = new Dictionary<string, DocumentAttribute>();

        private void Load()
        {
            try
            {
                var doc = new XmlDocument();

                if (_fromXml)
                {
                    doc.LoadXml(_xml);
                }
                else //from filename
                {
                    doc.Load(_xml);
                }

                XmlNode commonRegularExpressionListNode = doc.GetElementsByTagName("common-regexps")[0];
                CommonRegularExpressions = ParseCommonRegExps(commonRegularExpressionListNode);

                XmlNode directiveListNode = doc.GetElementsByTagName("directives")[0];
                Directives = ParseDirectives(directiveListNode);

                XmlNode commonAttributeListNode = doc.GetElementsByTagName("common-attributes")[0];
                CommonAttributes = ParseCommonAttributes(commonAttributeListNode);

                XmlNode globalAttributesListNode = doc.GetElementsByTagName("global-tag-attributes")[0];
                GlobalTagAttributes = ParseGlobalAttributes(globalAttributesListNode);

                XmlNode tagListNode = doc.GetElementsByTagName("tag-rules")[0];
                TagRules = ParseTagRules(tagListNode);

                XmlNode cssListNode = doc.GetElementsByTagName("css-rules")[0];
                CssRules = ParseCssRules(cssListNode);
            }
            catch (Exception ex)
            {
                throw new PolicyException("Policy parsing error", ex);
            }
        }

        public string GetRegularExpression(string name)
        {
            if (name == null || !CommonRegularExpressions.ContainsKey(name))
            {
                return null;
            }
            return CommonRegularExpressions[name];
        }

        public DocumentAttribute GetGlobalAttribute(string name) => GlobalTagAttributes.TryGetValue(name, out DocumentAttribute val) ? val : null;

        public DocumentTag GetTag(string tagName) => TagRules.TryGetValue(tagName, out DocumentTag value) ? value : null;

        public CssProperty GetCssProperty(string propertyName) => CssRules.TryGetValue(propertyName, out CssProperty value) ? value : null;

        public int GetDirectiveAsInt(string name, int defaultval) => GetDirective(name) != null ? int.Parse(GetDirective(name)) : defaultval;

        public string GetDirective(string name) => Directives.TryGetValue(name, out string value) ? value : null;

        #region Parsing methods

        private Dictionary<string, string> ParseDirectives(XmlNode directiveListNode)
        {
            XmlNodeList directiveNodes = directiveListNode.SelectNodes("directive");
            var directives = new Dictionary<string, string>();
            string name = "", value = "";
            foreach (XmlNode node in directiveNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    name = node.Attributes[0].Value;
                    value = node.Attributes[1].Value;
                    if (!directives.ContainsKey(name))
                    {
                        directives.Add(name, value);
                    }
                }
            }
            return directives;
        }

        private Dictionary<string, DocumentAttribute> ParseGlobalAttributes(XmlNode globalAttributeListNode)
        {
            XmlNodeList globalAttributeNodes = globalAttributeListNode.SelectNodes("attribute");
            var globalAttributes = new Dictionary<string, DocumentAttribute>(StringComparer.InvariantCultureIgnoreCase);

            //string _value = "";
            foreach (XmlNode node in globalAttributeNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    string name = node.Attributes[0].Value;

                    DocumentAttribute toAdd = CommonAttributes[name];
                    if (toAdd != null)
                    {
                        globalAttributes.Add(name, toAdd);
                    }
                    else
                    {
                        throw new PolicyException("Global attribute '" + name + "' was not defined in <common-attributes>");
                    }

                    //if (!globalAttributes.ContainsKey(_name))
                    //    globalAttributes.Add(_name, new AntiSamyPattern(_name, _value));
                }
            }
            return globalAttributes;
        }

        private Dictionary<string, string> ParseCommonRegExps(XmlNode commonRegularExpressionListNode)
        {
            XmlNodeList list = commonRegularExpressionListNode.SelectNodes("regexp");
            var commonRegularExpressions = new Dictionary<string, string>();
            foreach (XmlNode node in list)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    string name = node.Attributes[0].Value;
                    string value = node.Attributes[1].Value;
                    if (!commonRegularExpressions.ContainsKey(name))
                    {
                        commonRegularExpressions.Add(name, value);
                    }
                }
            }

            return commonRegularExpressions;
        }

        private Dictionary<string, DocumentAttribute> ParseCommonAttributes(XmlNode commonAttributeListNode)
        {
            XmlNodeList commonAttributeNodes = commonAttributeListNode.SelectNodes("attribute");
            var commonAttributes = new Dictionary<string, DocumentAttribute>(StringComparer.InvariantCultureIgnoreCase);

            foreach (XmlNode node in commonAttributeNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    var allowedRegExp = new List<string>();
                    XmlNodeList regExpListNode = node.SelectNodes("regexp-list");
                    if (regExpListNode != null && regExpListNode.Count > 0)
                    {
                        XmlNodeList regExpList = regExpListNode[0].SelectNodes("regexp");
                        foreach (XmlNode regExpNode in regExpList)
                        {
                            string regExpName = regExpNode.Attributes["name"]?.Value;
                            string value = regExpNode.Attributes["value"]?.Value;

                            //TODO: java version uses "Pattern" class to hold regular expressions.  I'm storing them as strings below
                            //find out if I need an equiv to pattern 
                            if (!string.IsNullOrEmpty(regExpName))
                            {
                                allowedRegExp.Add(GetRegularExpression(regExpName));
                            }
                            else
                            {
                                allowedRegExp.Add(RegexpBegin + value + RegexpEnd);
                            }
                        }
                    }

                    var allowedValues = new List<string>();
                    XmlNode literalListNode = node.SelectNodes("literal-list")[0];
                    if (literalListNode != null)
                    {
                        XmlNodeList literalNodes = literalListNode.SelectNodes("literal");
                        foreach (XmlNode literalNode in literalNodes)
                        {
                            string value = literalNode.Attributes["value"]?.Value;
                            if (!string.IsNullOrEmpty(value))
                            {
                                allowedValues.Add(value);
                            }
                            else if (literalNode.Value != null)
                            {
                                allowedValues.Add(literalNode.Value);
                            }
                        }
                    }

                    string onInvalid = node.Attributes["onInvalid"]?.Value;
                    string name = node.Attributes["name"]?.Value;
                    var attribute = new DocumentAttribute(name,
                        allowedRegExp,
                        allowedValues,
                        !string.IsNullOrEmpty(onInvalid) ? onInvalid : DefaultOninvalid,
                        node.Attributes["description"]?.Value);

                    commonAttributes.Add(name, attribute);
                }
            }
            return commonAttributes;
        }

        private Dictionary<string, DocumentTag> ParseTagRules(XmlNode tagAttributeListNode)
        {
            var tags = new Dictionary<string, DocumentTag>(StringComparer.InvariantCultureIgnoreCase);
            XmlNodeList tagList = tagAttributeListNode.SelectNodes("tag");
            foreach (XmlNode tagNode in tagList)
            {
                if (tagNode.NodeType == XmlNodeType.Element)
                {
                    string name = tagNode.Attributes["name"]?.Value;
                    string action = tagNode.Attributes["action"]?.Value;

                    var tag = new DocumentTag(name, action);

                    XmlNodeList attributeList = tagNode.SelectNodes("attribute");
                    foreach (XmlNode attributeNode in attributeList)
                    {
                        if (!attributeNode.HasChildNodes)
                        {
                            CommonAttributes.TryGetValue(attributeNode.Attributes["name"].Value, out DocumentAttribute attribute);

                            if (attribute != null)
                            {
                                string onInvalid = attributeNode.Attributes["onInvalid"]?.Value;
                                string description = attributeNode.Attributes["description"]?.Value;
                                if (!string.IsNullOrEmpty(onInvalid))
                                {
                                    attribute.OnInvalid = onInvalid;
                                }
                                if (!string.IsNullOrEmpty(description))
                                {
                                    attribute.Description = description;
                                }

                                tag.AddAllowedAttribute((DocumentAttribute)attribute.Clone());
                            }
                        }
                        else
                        {
                            var allowedRegExps = new List<string>();
                            XmlNode regExpListNode = attributeNode.SelectNodes("regexp-list")[0];
                            if (regExpListNode != null)
                            {
                                XmlNodeList regExpList = regExpListNode.SelectNodes("regexp");
                                foreach (XmlNode regExpNode in regExpList)
                                {
                                    string regExpName = regExpNode.Attributes["name"]?.Value;
                                    string value = regExpNode.Attributes["value"]?.Value;
                                    if (!string.IsNullOrEmpty(regExpName))
                                    {
                                        //AntiSamyPattern pattern = getRegularExpression(regExpName);
                                        string pattern = GetRegularExpression(regExpName);
                                        if (pattern != null)
                                        {
                                            allowedRegExps.Add(pattern);
                                        }

                                        //attribute.addAllowedRegExp(pattern.Pattern);
                                        else
                                        {
                                            throw new PolicyException("Regular expression '" + regExpName + "' was referenced as a common regexp in definition of '" + tag.Name + "', but does not exist in <common-regexp>");
                                        }
                                    }
                                    else if (!string.IsNullOrEmpty(value))
                                    {
                                        allowedRegExps.Add(RegexpBegin + value + RegexpEnd);
                                    }
                                }
                            }

                            var allowedValues = new List<string>();
                            XmlNode literalListNode = attributeNode.SelectNodes("literal-list")[0];
                            if (literalListNode != null)
                            {
                                XmlNodeList literalNodes = literalListNode.SelectNodes("literal");
                                foreach (XmlNode literalNode in literalNodes)
                                {
                                    string value = literalNode.Attributes["value"]?.Value;
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        allowedValues.Add(value);
                                    }
                                    else if (literalNode.Value != null)
                                    {
                                        allowedValues.Add(literalNode.Value);
                                    }
                                }
                            }

                            /* Custom attribute for this tag */
                            var attribute = new DocumentAttribute(attributeNode.Attributes["name"].Value,
                                allowedRegExps,
                                allowedValues,
                                attributeNode.Attributes["onInvalid"]?.Value,
                                attributeNode.Attributes["description"]?.Value);
                            tag.AddAllowedAttribute(attribute);
                        }
                    }

                    tags.Add(name, tag);
                }
            }
            return tags;
        }

        private Dictionary<string, CssProperty> ParseCssRules(XmlNode cssNodeList)
        {
            var properties = new Dictionary<string, CssProperty>(StringComparer.InvariantCultureIgnoreCase);
            XmlNodeList propertyNodes = cssNodeList.SelectNodes("property");

            /*
		    * Loop through the list of attributes and add them to the collection.
		    */
            foreach (XmlNode ele in propertyNodes)
            {
                string name = ele.Attributes["name"]?.Value;
                string description = ele.Attributes["description"]?.Value;
                string oninvalid = ele.Attributes["onInvalid"]?.Value;

                var allowedRegExps = new List<string>();
                XmlNode regExpListNode = ele.SelectNodes("regexp-list")[0];
                if (regExpListNode != null)
                {
                    XmlNodeList regExpList = regExpListNode.SelectNodes("regexp");
                    foreach (XmlNode regExpNode in regExpList)
                    {
                        string regExpName = regExpNode.Attributes["name"]?.Value;
                        string value = regExpNode.Attributes["value"]?.Value;

                        string pattern = GetRegularExpression(regExpName);
                        if (pattern != null)
                        {
                            allowedRegExps.Add(pattern);
                        }
                        else if (value != null)
                        {
                            allowedRegExps.Add(RegexpBegin + value + RegexpEnd);
                        }
                        else
                        {
                            throw new PolicyException("Regular expression '" + regExpName + "' was referenced as a common regexp in definition of '" + name + "', but does not exist in <common-regexp>");
                        }
                    }
                }

                var allowedLiterals = new List<string>();
                XmlNode literalListNode = ele.SelectNodes("literal-list")[0];
                if (literalListNode != null)
                {
                    XmlNodeList literalList = literalListNode.SelectNodes("literal");
                    foreach (XmlNode literalNode in literalList)
                    {
                        allowedLiterals.Add(literalNode.Attributes["value"].Value);
                    }
                }

                var shorthandRefs = new List<string>();
                XmlNode shorthandListNode = ele.SelectNodes("shorthand-list")[0];
                if (shorthandListNode != null)
                {
                    XmlNodeList shorthandList = shorthandListNode.SelectNodes("shorthand");
                    foreach (XmlNode shorthandNode in shorthandList)
                    {
                        shorthandRefs.Add(shorthandNode.Attributes["name"].Value);
                    }
                }

                properties.Add(name, new CssProperty(name,
                    allowedRegExps,
                    allowedLiterals,
                    shorthandRefs,
                    description,
                    !string.IsNullOrEmpty(oninvalid) ? oninvalid : DefaultOninvalid));
            }
            return properties;
        }

        #endregion

        #region Factory methods

        public static Policy Load(string filename, bool fromXml)
        {
            var policy = new Policy(filename, fromXml);
            policy.Load();
            return policy;
        }

        public static Policy FromFile(string filename) => Load(filename, false);

        public static Policy FromFile(FileInfo fileInfo) => FromFile(fileInfo.FullName);

        public static Policy FromXml(string xml) => Load(xml, true);

        #endregion
    }
}
