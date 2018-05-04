using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using AngleSharp.Dom.Css;
using AngleSharp.Extensions;
using AngleSharp.Parser.Css;

using AntiSamy.Model;

namespace AntiSamy
{
    internal class CssScanner
    {
        private readonly Policy _policy;
        private List<string> _errors = new List<string>();

        public CssScanner(Policy policy) => _policy = policy ?? throw new ArgumentNullException(nameof(policy));

        public AntiySamyResult ScanStyleSheet(string css, int maxinputsize)
        {
            DateTime start = DateTime.UtcNow;
            _errors = new List<string>();
            string cleanStyleSheet;

            try
            {
                ICssStyleSheet styleSheet;
                try
                {
                    styleSheet = new CssParser(new CssParserOptions
                    {
                        IsIncludingUnknownDeclarations = true,
                        IsIncludingUnknownRules = true,
                        IsToleratingInvalidConstraints = true,
                        IsToleratingInvalidValues = true
                    }).ParseStylesheet(css);
                }
                catch (Exception ex)
                {
                    throw new ParseException(ex.Message, ex);
                }

                cleanStyleSheet = ScanStyleSheet(styleSheet);
            }
            catch (ParseException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new ScanException("An error occured while scanning css", exception);
            }

            return new AntiySamyResult(start, cleanStyleSheet, _errors);
        }

        private string ScanStyleSheet(ICssStyleSheet styleSheet)
        {
            for (var i = 0; i < styleSheet.Rules.Length;)
            {
                ICssRule rule = styleSheet.Rules[i];
                if (!ScanStyleRule(rule))
                    styleSheet.RemoveAt(i);
                else
                    i++;
            }

            return styleSheet.ToCss();
        }

        private bool ScanStyleRule(ICssRule rule)
        {
            if (rule is ICssStyleRule styleRule)
            {
                ScanStyleDeclaration(styleRule.Style);
            }
            else if (rule is ICssGroupingRule groupingRule)
            {
                foreach (ICssRule childRule in groupingRule.Rules)
                {
                    ScanStyleRule(childRule);
                }
            }
            else if (rule is ICssPageRule pageRule)
            {
                ScanStyleDeclaration(pageRule.Style);
            }
            else if (rule is ICssKeyframesRule keyFramesRule)
            {
                foreach (ICssKeyframeRule childRule in keyFramesRule.Rules.OfType<ICssKeyframeRule>().ToList())
                {
                    ScanStyleRule(childRule);
                }
            }
            else if (rule is ICssKeyframeRule keyFrameRule)
            {
                ScanStyleDeclaration(keyFrameRule.Style);
            }
            else if (rule is ICssImportRule importRule)
            {
                //Dont allow import rules for now
                return false;
            }

            return true;
        }

        private void ScanStyleDeclaration(ICssStyleDeclaration styles)
        {
            var removingProperties = new List<Tuple<ICssProperty, string>>();

            var cssUrlTest = new Regex(@"[Uu][Rr\u0280][Ll\u029F]\s*\(\s*(['""]?)\s*([^'"")\s]+)\s*(['""]?)\s*", RegexOptions.Compiled);
            var dangerousCssExpressionTest = new Regex(@"[eE\uFF25\uFF45][xX\uFF38\uFF58][pP\uFF30\uFF50][rR\u0280\uFF32\uFF52][eE\uFF25\uFF45][sS\uFF33\uFF53]{2}[iI\u026A\uFF29\uFF49][oO\uFF2F\uFF4F][nN\u0274\uFF2E\uFF4E]", RegexOptions.Compiled);

            foreach (ICssProperty cssProperty in styles)
            {
                string key = DecodeCss(cssProperty.Name);
                string value = DecodeCss(cssProperty.Value);

                CssProperty allowedCssProperty = _policy.GetCssProperty(key);

                if (allowedCssProperty == null)
                {
                    removingProperties.Add(new Tuple<ICssProperty, string>(cssProperty, $"Css property \"{key}\" is not allowed"));
                    continue;
                }

                if (dangerousCssExpressionTest.IsMatch(value))
                {
                    removingProperties.Add(new Tuple<ICssProperty, string>(cssProperty, $"\"{value}\" is invalid css expression"));
                    continue;
                }

                ValidateValue(allowedCssProperty, cssProperty, value, removingProperties);

                MatchCollection urls = cssUrlTest.Matches(value);

                if (urls.Count > 0)
                {
                    var schemeRegex = new Regex(@"^\s*([^\/#]*?)(?:\:|&#0*58|&#x0*3a)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    if (!urls.Cast<Match>().All(u => schemeRegex.IsMatch(u.Value)))
                    {
                        removingProperties.Add(new Tuple<ICssProperty, string>(cssProperty, "Illegal url detected."));
                    }
                }
            }

            foreach (Tuple<ICssProperty, string> style in removingProperties)
            {
                styles.RemoveProperty(style.Item1.Name);
                _errors.Add(style.Item2);
            }
        }

        private void ValidateValue(CssProperty allowedCssProperty, ICssProperty cssProperty, string value, List<Tuple<ICssProperty, string>> removeStyles)
        {
            if (!allowedCssProperty.AllowedLiterals.Any(lit => lit.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                removeStyles.Add(new Tuple<ICssProperty, string>(cssProperty, $"\"{value}\" is not allowed literal"));
                return;
            }

            if (!allowedCssProperty.AllowedRegExps.Any(regex => new Regex(regex).IsMatch(value)))
            {
                removeStyles.Add(new Tuple<ICssProperty, string>(cssProperty, $"\"{value}\" is not allowed literal by regex"));
                return;
            }

            foreach (string shortHandRef in allowedCssProperty.ShorthandRefs)
            {
                CssProperty shorthand = _policy.GetCssProperty(shortHandRef);

                if (shorthand != null)
                {
                    ValidateValue(shorthand, cssProperty, value, removeStyles);
                }
            }
        }

        private static string DecodeCss(string css)
        {
            var cssComments = new Regex(@"/\*.*?\*/", RegexOptions.Compiled);
            var cssUnicodeEscapes = new Regex(@"\\([0-9a-fA-F]{1,6})\s?|\\([^\r\n\f0-9a-fA-F'""{};:()#*])", RegexOptions.Compiled);

            string r = cssUnicodeEscapes.Replace(css, m =>
            {
                if (m.Groups[1].Success)
                {
                    return ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString();
                }
                string t = m.Groups[2].Value;
                return t == "\\" ? @"\\" : t;
            });

            r = cssComments.Replace(r, m => "");

            return r;
        }
    }
}
