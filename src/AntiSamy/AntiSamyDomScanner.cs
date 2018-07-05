using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AntiSamy.Model;

using HtmlAgilityPack;

namespace AntiSamy
{
    public sealed class AntiSamyDomScanner
    {
        private readonly List<string> _errorMessages = new List<string>();

        private readonly Policy _policy;

        public AntiSamyDomScanner(Policy policy) => _policy = policy;

        public AntiySamyResult Scan(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            html = html.Replace("&nbsp;", char.Parse("\u00a0").ToString());
            html = StripNonValidXmlCharacters(html);
            int maxInputSize = Policy.DefaultMaxInputSize;

            try
            {
                maxInputSize = _policy.GetDirectiveAsInt("maxInputSize", int.MaxValue);
            }
            catch (FormatException fe)
            {
                Console.WriteLine("Format Exception: " + fe);
            }

            if (maxInputSize < html.Length)
            {
                throw new ScanException("File size [" + html.Length + "] is larger than maximum [" + maxInputSize + "]");
            }

            DateTime start = DateTime.Now;

            if (!HtmlNode.ElementsFlags.ContainsKey("iframe"))
            {
                HtmlNode.ElementsFlags.Add("iframe", HtmlElementFlag.Empty);
            }
            HtmlNode.ElementsFlags.Remove("form");

            var doc = new HtmlDocument();
            doc.LoadHtml(html.Replace(Environment.NewLine, string.Empty).Replace("\t", string.Empty));

            doc.OptionAutoCloseOnEnd = true;
            doc.OptionOutputAsXml = true;

            EvaluateNodeCollection(doc.DocumentNode.ChildNodes);

            string finalCleanHtml = doc.DocumentNode.InnerHtml;

            return new AntiySamyResult(start, finalCleanHtml, _errorMessages);
        }

        private void EvaluateNodeCollection(HtmlNodeCollection nodes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                HtmlNode node = nodes[i];
                EvaluateNode(node);

                if (node.ParentNode == null)
                {
                    i--;
                }
            }
        }

        private void EvaluateNode(HtmlNode node)
        {
            int maxinputsize = _policy.GetDirectiveAsInt("maxInputSize", int.MaxValue);

            HtmlNode parentNode = node.ParentNode;
            string tagName = node.Name;

            if (tagName.ToLower().Equals("#text")) // || tagName.ToLower().Equals("#comment"))
            {
                return;
            }

            DocumentTag tag = _policy.GetTag(tagName.ToLower());

            if (tag == null || Consts.TagActions.FILTER.Equals(tag.Action))
            {
                var errBuff = new StringBuilder();
                if (tagName.Trim().Equals(""))
                {
                    errBuff.Append("An unprocessable ");
                }
                else
                {
                    errBuff.Append("The \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName.ToLower()) + "\" ");
                }

                errBuff.Append("tag has been filtered for security reasons. The contents of the tag will ");
                errBuff.Append("remain in place.");

                _errorMessages.Add(errBuff.ToString());

                EvaluateNodeCollection(node.ChildNodes);
               
                PromoteChildren(node);
            }
            else if (Consts.TagActions.VALIDATE.Equals(tag.Action))
            {
                if ("style".Equals(tagName.ToLower()) && _policy.GetTag("style") != null)
                {
                    ScanCss(node, parentNode, maxinputsize, false);
                }

                for (var currentAttributeIndex = 0; currentAttributeIndex < node.Attributes.Count; currentAttributeIndex++)
                {
                    HtmlAttribute htmlAttribute = node.Attributes[currentAttributeIndex];

                    string name = htmlAttribute.Name;
                    string value = htmlAttribute.Value;


                    DocumentAttribute allowwdAttr = tag.GetAttributeByName(name) ?? _policy.GetGlobalAttribute(name);
                    if (allowwdAttr == null)
                    {
                        var errBuff = new StringBuilder();

                        errBuff.Append("The \"" + HtmlEntityEncoder.HtmlEntityEncode(name));
                        errBuff.Append("\" attribute of the \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "\" tag has been removed for security reasons. ");
                        errBuff.Append("This removal should not affect the display of the HTML submitted.");

                        _errorMessages.Add(errBuff.ToString());
                        node.Attributes.Remove(name);
                        currentAttributeIndex--;
                    }
                    else
                    {

                        if ("style".Equals(name.ToLower()))
                        {
                            ScanCss(node, parentNode, maxinputsize, true);
                        }
                        else
                        {
                            if (!allowwdAttr.AllowedValues.Any() && !allowwdAttr.AllowedRegExps.Any())
                            {
                                continue;
                            }

                            var isAttributeValid = false;
                            //try to find out how robust this is - do I need to do this in a loop?
                            value = HtmlEntity.DeEntitize(value);

                            foreach (string allowedValue in allowwdAttr.AllowedValues)
                            {
                                if (isAttributeValid)
                                {
                                    break;
                                }

                                if (allowedValue != null && allowedValue.ToLower().Equals(value.ToLower()))
                                {
                                    isAttributeValid = true;
                                }
                            }

                            foreach (string ptn in allowwdAttr.AllowedRegExps)
                            {
                                if (isAttributeValid)
                                {
                                    break;
                                }
                                string pattern = "^" + ptn + "$";
                                Match m = Regex.Match(value, pattern);
                                if (m.Success)
                                {
                                    isAttributeValid = true;
                                }
                            }

                            if (!isAttributeValid)
                            {
                                string onInvalidAction = allowwdAttr.OnInvalid;
                                var errBuff = new StringBuilder();

                                errBuff.Append("The \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "\" tag contained an attribute that we couldn't process. ");
                                errBuff.Append("The \"" + HtmlEntityEncoder.HtmlEntityEncode(name) + "\" attribute had a value of <u>" + HtmlEntityEncoder.HtmlEntityEncode(value) + "</u>. ");
                                errBuff.Append("This value could not be accepted for security reasons. We have chosen to ");

                                //Console.WriteLine(policy);

                                if (Consts.OnInvalidActions.REMOVE_TAG.Equals(onInvalidAction))
                                {
                                    parentNode.RemoveChild(node);
                                    errBuff.Append("remove the \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "\" tag and its contents in order to process this input. ");
                                }
                                else if (Consts.OnInvalidActions.FILTER_TAG.Equals(onInvalidAction))
                                {
                                   
                                    EvaluateNodeCollection(node.ChildNodes);

                                    PromoteChildren(node);

                                    errBuff.Append("filter the \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "\" tag and leave its contents in place so that we could process this input.");
                                }
                                else
                                {
                                    node.Attributes.Remove(allowwdAttr.Name);
                                    currentAttributeIndex--;
                                    errBuff.Append("remove the \"" + HtmlEntityEncoder.HtmlEntityEncode(name) + "\" attribute from the tag and leave everything else in place so that we could process this input.");
                                }

                                _errorMessages.Add(errBuff.ToString());

                                if ("removeTag".Equals(onInvalidAction) || "filterTag".Equals(onInvalidAction))
                                {
                                    return; // can't process any more if we remove/filter the tag	
                                }
                            }
                        }
                    }
                }

                EvaluateNodeCollection(node.ChildNodes);
            }
            else if ("truncate".Equals(tag.Action))// || Consts.TagActions.REMOVE.Equals(tag.Action))
            {
                Console.WriteLine("truncate");
                HtmlAttributeCollection nnmap = node.Attributes;

                while (nnmap.Count > 0)
                {
                    var errBuff = new StringBuilder();

                    errBuff.Append("The \"" + HtmlEntityEncoder.HtmlEntityEncode(nnmap[0].Name));
                    errBuff.Append("\" attribute of the \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "\" tag has been removed for security reasons. ");
                    errBuff.Append("This removal should not affect the display of the HTML submitted.");
                    node.Attributes.Remove(nnmap[0].Name);
                    _errorMessages.Add(errBuff.ToString());
                }

                HtmlNodeCollection cList = node.ChildNodes;

                var i = 0;
                var j = 0;
                int length = cList.Count;

                while (i < length)
                {
                    HtmlNode nodeToRemove = cList[j];
                    if (nodeToRemove.NodeType != HtmlNodeType.Text && nodeToRemove.NodeType != HtmlNodeType.Comment)
                    {
                        node.RemoveChild(nodeToRemove);
                    }
                    else
                    {
                        j++;
                    }
                    i++;
                }
            }
            else
            {
                _errorMessages.Add("The \"" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "\" tag has been removed for security reasons.");
                parentNode.RemoveChild(node);
            }
        }

        private void ScanCss(HtmlNode node, HtmlNode parentNode, int maxinputsize, bool fromStyleAttribute)
        {
            var styleScanner = new CssScanner(_policy);
            try
            {
                AntiySamyResult cssResult = null;
                //if (node.Attributes.Contains("style"))
                if (fromStyleAttribute)
                {
                    cssResult = styleScanner.ScanStyleSheet(node.Attributes["style"].Value, maxinputsize, fromStyleAttribute);
                    if (string.IsNullOrWhiteSpace(cssResult.CleanHtml))
                    {
                        node.Attributes["style"].Remove();
                    }
                    else
                        node.Attributes["style"].Value = cssResult.CleanHtml;
                }
                else if (node.FirstChild != null)
                {
                    cssResult = styleScanner.ScanStyleSheet(node.FirstChild.InnerHtml, maxinputsize, fromStyleAttribute);
                    node.FirstChild.InnerHtml = cssResult.CleanHtml;
                }
                if (cssResult != null && cssResult.ErrorMessages.Any())
                    _errorMessages.AddRange(cssResult.ErrorMessages);
            }
            catch (ParseException e)
            {
                parentNode.RemoveChild(node);
                _errorMessages.Add($"Css could not be parsed. {e}");
            }
        }

        private static void PromoteChildren(HtmlNode node)
        {
            HtmlNodeCollection nodeList = node.ChildNodes;
            HtmlNode parent = node.ParentNode;

            while (nodeList.Count > 0)
            {
                HtmlNode removeNode = node.RemoveChild(nodeList[0]);
                parent.InsertBefore(removeNode, node);
            }

            parent.RemoveChild(node);
        }

        private static string StripNonValidXmlCharacters(string inRenamed)
        {
            var outRenamed = new StringBuilder(); // Used to hold the output.

            if (inRenamed == null || "".Equals(inRenamed))
            {
                return ""; // vacancy test.
            }
            for (var i = 0; i < inRenamed.Length; i++)
            {
                char current = inRenamed[i]; // Used to reference the current character.
                if (current == 0x9 || current == 0xA || current == 0xD || current >= 0x20 && current <= 0xD7FF || current >= 0xE000 && current <= 0xFFFD || current >= 0x10000 && current <= 0x10FFFF)
                {
                    outRenamed.Append(current);
                }
            }

            return outRenamed.ToString();
        }
    }
}
