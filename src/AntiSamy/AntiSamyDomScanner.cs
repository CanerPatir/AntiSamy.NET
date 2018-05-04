using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using AntiSamy.Model;

using HtmlAgilityPack;

namespace AntiSamy
{
    public sealed class AntiSamyDomScanner
    {
        public const string DefaultEncodingAlgorithm = "UTF-8";

        private readonly List<string> _errorMessages = new List<string>();

        private readonly Policy _policy;

        private int _num;

        public AntiSamyDomScanner(Policy policy) => _policy = policy;

        public AntiySamyResult Scan(string html, string inputEncoding, string outputEncoding)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            //had problems with the &nbsp; getting double encoded, so this converts it to a literal space.  
            //this may need to be changed.
            html = html.Replace("&nbsp;", char.Parse("\u00a0").ToString());

            //We have to replace any invalid XML characters

            html = StripNonValidXmlCharacters(html);

            //holds the maximum input size for the incoming fragment
            int maxInputSize = Policy.DefaultMaxInputSize;

            //grab the size specified in the config file
            try
            {
                maxInputSize = _policy.GetDirectiveAsInt("maxInputSize", int.MaxValue);
            }
            catch (FormatException fe)
            {
                Console.WriteLine("Format Exception: " + fe);
            }

            //ensure our input is less than the max
            if (maxInputSize < html.Length)
            {
                throw new ScanException("File size [" + html.Length + "] is larger than maximum [" + maxInputSize + "]");
            }

            //grab start time (to be put in the result set along with end time)
            DateTime start = DateTime.Now;

            //fixes some weirdness in HTML agility
            if (!HtmlNode.ElementsFlags.ContainsKey("iframe"))
            {
                HtmlNode.ElementsFlags.Add("iframe", HtmlElementFlag.Empty);
            }
            HtmlNode.ElementsFlags.Remove("form");

            //Let's parse the incoming HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            //add closing tags
            doc.OptionAutoCloseOnEnd = true;

            //enforces XML rules, encodes big 5
            doc.OptionOutputAsXml = true;

            //loop through every node now, and enforce the rules held in the policy object
            for (var i = 0; i < doc.DocumentNode.ChildNodes.Count; i++)
            {
                //grab current node
                HtmlNode tmp = doc.DocumentNode.ChildNodes[i];

                //this node can hold other nodes, so recursively validate
                RecursiveValidateTag(tmp);

                if (tmp.ParentNode == null)
                {
                    i--;
                }
            }

            string finalCleanHtml = doc.DocumentNode.InnerHtml;

            return new AntiySamyResult(start, finalCleanHtml, _errorMessages);
        }

        private void RecursiveValidateTag(HtmlNode node)
        {
            int maxinputsize = _policy.GetDirectiveAsInt("maxInputSize", int.MaxValue);

            _num++;

            HtmlNode parentNode = node.ParentNode;
            HtmlNode tmp = null;
            string tagName = node.Name;

            //check this out
            //might not be robust enough
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
                    errBuff.Append("The <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName.ToLower()) + "</b> ");
                }

                errBuff.Append("tag has been filtered for security reasons. The contents of the tag will ");
                errBuff.Append("remain in place.");

                _errorMessages.Add(errBuff.ToString());

                for (var i = 0; i < node.ChildNodes.Count; i++)
                {
                    tmp = node.ChildNodes[i];
                    RecursiveValidateTag(tmp);

                    if (tmp.ParentNode == null)
                    {
                        i--;
                    }
                }
                PromoteChildren(node);
            }
            else if (Consts.TagActions.VALIDATE.Equals(tag.Action))
            {
                if ("style".Equals(tagName.ToLower()) && _policy.GetTag("style") != null)
                {
                    ScanCss(node, parentNode, maxinputsize);
                }

                for (var currentAttributeIndex = 0; currentAttributeIndex < node.Attributes.Count; currentAttributeIndex++)
                {
                    HtmlAttribute attribute = node.Attributes[currentAttributeIndex];

                    string name = attribute.Name;
                    string value = attribute.Value;

                    DocumentAttribute attr = tag.GetAttributeByName(name) ?? _policy.GetGlobalAttribute(name);

                    var isAttributeValid = false;

                    if ("style".Equals(name.ToLower()) && attr != null)
                    {
                        ScanCss(node, parentNode, maxinputsize);
                    }
                    if (attr != null)
                    {
                        //try to find out how robust this is - do I need to do this in a loop?
                        value = HtmlEntity.DeEntitize(value);

                        foreach (string allowedValue in attr.AllowedValues)
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

                        foreach (string ptn in attr.AllowedRegExps)
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
                            string onInvalidAction = attr.OnInvalid;
                            var errBuff = new StringBuilder();

                            errBuff.Append("The <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "</b> tag contained an attribute that we couldn't process. ");
                            errBuff.Append("The <b>" + HtmlEntityEncoder.HtmlEntityEncode(name) + "</b> attribute had a value of <u>" + HtmlEntityEncoder.HtmlEntityEncode(value) + "</u>. ");
                            errBuff.Append("This value could not be accepted for security reasons. We have chosen to ");

                            //Console.WriteLine(policy);

                            if (Consts.OnInvalidActions.REMOVE_TAG.Equals(onInvalidAction))
                            {
                                parentNode.RemoveChild(node);
                                errBuff.Append("remove the <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "</b> tag and its contents in order to process this input. ");
                            }
                            else if (Consts.OnInvalidActions.FILTER_TAG.Equals(onInvalidAction))
                            {
                                for (var i = 0; i < node.ChildNodes.Count; i++)
                                {
                                    tmp = node.ChildNodes[i];
                                    RecursiveValidateTag(tmp);
                                    if (tmp.ParentNode == null)
                                    {
                                        i--;
                                    }
                                }

                                PromoteChildren(node);

                                errBuff.Append("filter the <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "</b> tag and leave its contents in place so that we could process this input.");
                            }
                            else if (Consts.OnInvalidActions.REMOVE_ATTRIBUTE.Equals(onInvalidAction))
                            {
                                node.Attributes.Remove(attr.Name);
                                currentAttributeIndex--;
                                errBuff.Append("remove the <b>" + HtmlEntityEncoder.HtmlEntityEncode(name) + "</b> attribute from the tag and leave everything else in place so that we could process this input.");
                            }

                            _errorMessages.Add(errBuff.ToString());

                            if ("removeTag".Equals(onInvalidAction) || "filterTag".Equals(onInvalidAction))
                            {
                                return; // can't process any more if we remove/filter the tag	
                            }
                        }
                    }
                    else
                    {
                        var errBuff = new StringBuilder();

                        errBuff.Append("The <b>" + HtmlEntityEncoder.HtmlEntityEncode(name));
                        errBuff.Append("</b> attribute of the <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "</b> tag has been removed for security reasons. ");
                        errBuff.Append("This removal should not affect the display of the HTML submitted.");

                        _errorMessages.Add(errBuff.ToString());
                        node.Attributes.Remove(name);
                        currentAttributeIndex--;
                    }
                }

                for (var i = 0; i < node.ChildNodes.Count; i++)
                {
                    tmp = node.ChildNodes[i];
                    RecursiveValidateTag(tmp);
                    if (tmp.ParentNode == null)
                    {
                        i--;
                    }
                }
            }
            else if ("truncate".Equals(tag.Action) || Consts.TagActions.REMOVE.Equals(tag.Action))
            {
                Console.WriteLine("truncate");
                HtmlAttributeCollection nnmap = node.Attributes;

                while (nnmap.Count > 0)
                {
                    var errBuff = new StringBuilder();

                    errBuff.Append("The <b>" + HtmlEntityEncoder.HtmlEntityEncode(nnmap[0].Name));
                    errBuff.Append("</b> attribute of the <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "</b> tag has been removed for security reasons. ");
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
                _errorMessages.Add("The <b>" + HtmlEntityEncoder.HtmlEntityEncode(tagName) + "</b> tag has been removed for security reasons.");
                parentNode.RemoveChild(node);
            }
        }

        private void ScanCss(HtmlNode node, HtmlNode parentNode, int maxinputsize)
        {
            var styleScanner = new CssScanner(_policy);
            try
            {
                AntiySamyResult cssResult;
                if (node.Attributes.Contains("style"))
                {
                    cssResult = styleScanner.ScanStyleSheet(node.Attributes["style"].Value, maxinputsize);
                    node.Attributes["style"].Value = cssResult.CleanHtml;
                }
                else
                {
                    cssResult = styleScanner.ScanStyleSheet(node.FirstChild.InnerHtml, maxinputsize);
                    node.FirstChild.InnerHtml = cssResult.CleanHtml;
                }
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
