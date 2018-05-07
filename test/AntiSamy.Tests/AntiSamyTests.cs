using FluentAssertions;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace AntiSamy.Tests
{
    public class AntiSamyTests : TestBase
    {
        private static readonly string[] BASE64_BAD_XML_STRINGS = new string[]{
            // first string is
            // "<a - href=\"http://www.owasp.org\">click here</a>"
            "PGEgLSBocmVmPSJodHRwOi8vd3d3Lm93YXNwLm9yZyI+Y2xpY2sgaGVyZTwvYT4=",
            // the rest are randomly generated 300 byte sequences which generate
            // parser errors, turned into Strings
            "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",
        };

        private AntiSamy _sut = new AntiSamy();


        [Fact]
        public void scriptAttacks()
        {
            _sut.Scan("test<script>alert(document.cookie)</script>", TestPolicy).CleanHtml.Contains("script").Should().BeFalse();

            _sut.Scan("<<<><<script src=http://fake-evil.ru/test.js>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<script<script src=http://fake-evil.ru/test.js>>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<SCRIPT/XSS SRC=\"http://ha.ckers.org/xss.js\"></SCRIPT>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<BODY onload!#$%&()*~+-_.,:;?@[/|\\]^`=alert(\"XSS\")>", TestPolicy).CleanHtml.Contains("onload").Should().BeFalse();

            _sut.Scan("<BODY ONLOAD=alert('XSS')>", TestPolicy).CleanHtml.Contains("alert").Should().BeFalse();

            _sut.Scan("<iframe src=http://ha.ckers.org/scriptlet.html <", TestPolicy).CleanHtml.Contains("<iframe").Should().BeFalse();

            _sut.Scan("<INPUT TYPE=\"IMAGE\" SRC=\"javascript:alert('XSS');\">", TestPolicy).CleanHtml.Contains("src").Should().BeFalse();

            _sut.Scan("<a onblur=\"alert(secret)\" href=\"http://www.google.com\">Google</a>", TestPolicy);
        }

        [Fact]
        public void imgAttacks()
        {
            _sut.Scan("<img src=\"http://www.myspace.com/img.gif\"/>", TestPolicy).CleanHtml.Contains("<img").Should().BeTrue();

            _sut.Scan("<img src=javascript:alert(document.cookie)>", TestPolicy).CleanHtml.Contains("<img").Should().BeFalse();

            _sut.Scan("<IMG SRC=&#106;&#97;&#118;&#97;&#115;&#99;&#114;&#105;&#112;&#116;&#58;&#97;&#108;&#101;&#114;&#116;&#40;&#39;&#88;&#83;&#83;&#39;&#41;>", TestPolicy)
                     .CleanHtml.Contains("<img").Should().BeFalse();


            //_sut.Scan("<IMG SRC='&#0000106&#0000097&#0000118&#0000097&#0000115&#0000099&#0000114&#0000105&#0000112&#0000116&#0000058&#0000097&#0000108&#0000101&#0000114&#0000116&#0000040&#0000039&#0000088&#0000083&#0000083&#0000039&#0000041'>", policy)
            //        .CleanHtml.Contains("<img").Should().BeFalse();


            _sut.Scan("<IMG SRC=\"jav&#x0D;ascript:alert('XSS');\">", TestPolicy).CleanHtml.Contains("alert").Should().BeFalse();

            string s = _sut.Scan("<IMG SRC=&#0000106&#0000097&#0000118&#0000097&#0000115&#0000099&#0000114&#0000105&#0000112&#0000116&#0000058&#0000097&#0000108&#0000101&#0000114&#0000116&#0000040&#0000039&#0000088&#0000083&#0000083&#0000039&#0000041>", TestPolicy).CleanHtml;
            (s.Length == 0 || s.Contains("&amp;")).Should().BeTrue();


            _sut.Scan("<IMG SRC=&#x6A&#x61&#x76&#x61&#x73&#x63&#x72&#x69&#x70&#x74&#x3A&#x61&#x6C&#x65&#x72&#x74&#x28&#x27&#x58&#x53&#x53&#x27&#x29>", TestPolicy);

            _sut.Scan("<IMG SRC=\"javascript:alert('XSS')\"", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<IMG LOWSRC=\"javascript:alert('XSS')\">", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<BGSOUND SRC=\"javascript:alert('XSS');\">", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();
        }

        [Fact]
        public void hrefAttacks()
        {
            _sut.Scan("<LINK REL=\"stylesheet\" HREF=\"javascript:alert('XSS');\">", TestPolicy).CleanHtml.Contains("href").Should().BeFalse();

            _sut.Scan("<LINK REL=\"stylesheet\" HREF=\"http://ha.ckers.org/xss.css\">", TestPolicy).CleanHtml.Contains("href").Should().BeFalse();

            _sut.Scan("<STYLE>@import'http://ha.ckers.org/xss.css';</STYLE>", TestPolicy).CleanHtml.Contains("ha.ckers.org").Should().BeFalse();

            _sut.Scan("<STYLE>BODY{-moz-binding:url(\"http://ha.ckers.org/xssmoz.xml#xss\")}</STYLE>", TestPolicy).CleanHtml.Contains("ha.ckers.org").Should().BeFalse();

            _sut.Scan("<STYLE>li {list-style-image: url(\"javascript:alert('XSS')\");}</STYLE><UL><LI>XSS", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<IMG SRC='vbscript:msgbox(\"XSS\")'>", TestPolicy).CleanHtml.Contains("vbscript").Should().BeFalse();

            _sut.Scan("<META HTTP-EQUIV=\"refresh\" CONTENT=\"0; URL=http://;URL=javascript:alert('XSS');\">", TestPolicy).CleanHtml.Contains("<meta").Should().BeFalse();

            _sut.Scan("<META HTTP-EQUIV=\"refresh\" CONTENT=\"0;url=javascript:alert('XSS');\">", TestPolicy).CleanHtml.Contains("<meta").Should().BeFalse();

            _sut.Scan("<META HTTP-EQUIV=\"refresh\" CONTENT=\"0;url=data:text/html;base64,PHNjcmlwdD5hbGVydCgnWFNTJyk8L3NjcmlwdD4K\">", TestPolicy).CleanHtml.Contains("<meta").Should().BeFalse();

            _sut.Scan("<FRAMESET><FRAME SRC=\"javascript:alert('XSS');\"></FRAMESET>", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<TABLE BACKGROUND=\"javascript:alert('XSS')\">", TestPolicy).CleanHtml.Contains("background").Should().BeFalse();

            _sut.Scan("<TABLE><TD BACKGROUND=\"javascript:alert('XSS')\">", TestPolicy).CleanHtml.Contains("background").Should().BeFalse();

            _sut.Scan("<DIV STYLE=\"background-image: url(javascript:alert('XSS'))\">", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<DIV STYLE=\"width: expression(alert('XSS'));\">", TestPolicy).CleanHtml.Contains("alert").Should().BeFalse();

            _sut.Scan("<IMG STYLE=\"xss:expr/*XSS*/ession(alert('XSS'))\">", TestPolicy).CleanHtml.Contains("alert").Should().BeFalse();

            _sut.Scan("<STYLE>@im\\port'\\ja\\vasc\\ript:alert(\"XSS\")';</STYLE>", TestPolicy).CleanHtml.Contains("ript:alert").Should().BeFalse();

            _sut.Scan("<BASE HREF=\"javascript:alert('XSS');//\">", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<BaSe hReF=\"http://arbitrary.com/\">", TestPolicy).CleanHtml.Contains("<base").Should().BeFalse();

            _sut.Scan("<OBJECT TYPE=\"text/x-scriptlet\" DATA=\"http://ha.ckers.org/scriptlet.html\"></OBJECT>", TestPolicy).CleanHtml.Contains("<object").Should().BeFalse();

            _sut.Scan("<OBJECT classid=clsid:ae24fdae-03c6-11d1-8b76-0080c744f389><param name=url value=javascript:alert('XSS')></OBJECT>", TestPolicy).CleanHtml.Contains("jaascript").Should().BeFalse();

            _sut.Scan("<EMBED SRC=\"http://ha.ckers.org/xss.swf\" AllowScriptAccess=\"always\"></EMBED>", TestPolicy).CleanHtml.Contains("<embed").Should().BeFalse();

            _sut.Scan("<EMBED SRC=\"data:image/svg+xml;base64,PHN2ZyB4bWxuczpzdmc9Imh0dH A6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcv MjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hs aW5rIiB2ZXJzaW9uPSIxLjAiIHg9IjAiIHk9IjAiIHdpZHRoPSIxOTQiIGhlaWdodD0iMjAw IiBpZD0ieHNzIj48c2NyaXB0IHR5cGU9InRleHQvZWNtYXNjcmlwdCI+YWxlcnQoIlh TUyIpOzwvc2NyaXB0Pjwvc3ZnPg==\" type=\"image/svg+xml\" AllowScriptAccess=\"always\"></EMBED>", TestPolicy).CleanHtml.Contains("<embed").Should().BeFalse();

            _sut.Scan("<SCRIPT a=\">\" SRC=\"http://ha.ckers.org/xss.js\"></SCRIPT>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<SCRIPT a=\">\" '' SRC=\"http://ha.ckers.org/xss.js\"></SCRIPT>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<SCRIPT a=`>` SRC=\"http://ha.ckers.org/xss.js\"></SCRIPT>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<SCRIPT a=\">'>\" SRC=\"http://ha.ckers.org/xss.js\"></SCRIPT>", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<SCRIPT>document.write(\"<SCRI\");</SCRIPT>PT SRC=\"http://ha.ckers.org/xss.js\"></SCRIPT>", TestPolicy).CleanHtml.Contains("script").Should().BeFalse();

            _sut.Scan("<SCRIPT SRC=http://ha.ckers.org/xss.js", TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();

            _sut.Scan("<div/style=&#92&#45&#92&#109&#111&#92&#122&#92&#45&#98&#92&#105&#92&#110&#100&#92&#105&#110&#92&#103:&#92&#117&#114&#108&#40&#47&#47&#98&#117&#115&#105&#110&#101&#115&#115&#92&#105&#92&#110&#102&#111&#46&#99&#111&#46&#117&#107&#92&#47&#108&#97&#98&#115&#92&#47&#120&#98&#108&#92&#47&#120&#98&#108&#92&#46&#120&#109&#108&#92&#35&#120&#115&#115&#41&>", TestPolicy).CleanHtml.Contains("style").Should().BeFalse();

            _sut.Scan("<a href='aim: &c:\\windows\\system32\\calc.exe' ini='C:\\Documents and Settings\\All Users\\Start Menu\\Programs\\Startup\\pwnd.bat'>", TestPolicy).CleanHtml.Contains("aim.exe").Should().BeFalse();

            _sut.Scan("<!--\n<A href=\n- --><a href=javascript:alert:document.domain>test-->", TestPolicy).CleanHtml.Contains("javascript").Should().BeFalse();

            _sut.Scan("<a></a style=\"\"xx:expr/**/ession(document.appendChild(document.createElement('script')).src='http://h4k.in/i.js')\">", TestPolicy).CleanHtml.Contains("document").Should().BeFalse();

            _sut.Scan("<IFRAME SRC=\"javascript:alert('XSS');\"></IFRAME>", TestPolicy).CleanHtml.Contains("iframe").Should().BeFalse();
        }

        [Fact]
        public void IllegalXML()
        {
            foreach (string BASE64_BAD_XML_STRING in BASE64_BAD_XML_STRINGS)
            {
                try
                {

                    string testStr = Encoding.UTF8.GetString(Convert.FromBase64String(BASE64_BAD_XML_STRING));
                    _sut.Scan(testStr, TestPolicy);

                }
                catch (ScanException)
                {
                    // still success!
                }
            }

            _sut.Scan("<style>", TestPolicy).Should().NotBeNull();
        }

        [Fact]
        public void issue12()
        {

            /*
               * issues 12 (and 36, which was similar). empty tags cause display
               * problems/"formjacking"
               */


            var p = new Regex(".*<strong(\\s*)/>.*");
            string s1 = _sut.Scan("<br ><strong></strong><a>hello world</a><b /><i/><hr>", TestPolicy).CleanHtml;

            p.IsMatch(s1).Should().BeFalse();

            p = new Regex(".*<b(\\s*)/>.*");
            p.IsMatch(s1).Should().BeFalse();

            p = new Regex(".*<i(\\s*)/>.*");
            p.IsMatch(s1).Should().BeFalse();

            (s1.Contains("<hr />") || s1.Contains("<hr/>")).Should().BeTrue();
        }

        [Fact]
        public void issue20()
        {
            string s = _sut.Scan("<b><i>Some Text</b></i>", TestPolicy).CleanHtml;
            s.Contains("<i />").Should().BeFalse();
        }

        [Fact]
        public void issue25()
        {
            var s = "<div style=\"margin: -5em\">Test</div>";
            var expected = "<div>Test</div>";

            string crDom = _sut.Scan(s, TestPolicy).CleanHtml;
            crDom.Should().BeEquivalentTo(expected);
        }


        [Fact]
        public void issue28()
        {
            string s1 = _sut.Scan("<div style=\"font-family: serif\">Test</div>", TestPolicy).CleanHtml;
            s1.Contains("font-family").Should().BeTrue();
        }

        [Fact(Skip = "XHTML not suported")]
        public void issue29()
        {
            /* issue #29 - missing quotes around properties with spaces */
            var s = "<style type=\"text/css\"><![CDATA[P {\n	font-family: \"Arial Unicode MS\";\n}\n]]></style>";
            AntiySamyResult result = _sut.Scan(s, TestPolicy);
            s.Should().BeEquivalentTo(result.CleanHtml);
        }

        [Fact(Skip = "XHTML not suported")]
        public void issue30()
        {

            var s = "<style type=\"text/css\"><![CDATA[P { margin-bottom: 0.08in; } ]]></style>";

            _sut.Scan(s, TestPolicy);

            /* followup - does the patch fix multiline CSS? */
            var s2 = "<style type=\"text/css\"><![CDATA[\r\nP {\r\n margin-bottom: 0.08in;\r\n}\r\n]]></style>";
            AntiySamyResult cr = _sut.Scan(s2, TestPolicy);
            "<style type=\"text/css\"><![CDATA[P {\n\tmargin-bottom: 0.08in;\n}\n]]></style>".Should().BeEquivalentTo(cr.CleanHtml);

            /* next followup - does non-CDATA parsing still work? */

            //var s3 = "<style>P {\n\tmargin-bottom: 0.08in;\n}\n";
            //policy.UseXhtml = false;
            //cr = _sut.Scan(s3, );
            //"<style>P {\n\tmargin-bottom: 0.08in;\n}\n</style>\n".Should().BeEquivalentTo(cr.CleanHtml);
        }

        [Fact(Skip = "onUnknownTag not supported")]
        public void isssue31()
        {

            var test = "<b><u><g>foo";
            //Policy revised = policy.cloneWithDirective("onUnknownTag", "encode");

            AntiySamyResult cr = _sut.Scan(test, TestPolicy);
            string s = cr.CleanHtml;
            s.Contains("&lt;g&gt;").Should().BeTrue();
        }

        [Fact]
        public void issue37()
        {
            string dirty = "<a onblur=\"try {parent.deselectBloggerImageGracefully();}" + "catch(e) {}\""
                    + "href=\"http://www.charityadvantage.com/ChildrensmuseumEaston/images/BookswithBill.jpg\"><img" + "style=\"FLOAT: right; MARGIN: 0px 0px 10px 10px; WIDTH: 150px; CURSOR:"
                    + "hand; HEIGHT: 100px\" alt=\"\"" + "src=\"http://www.charityadvantage.com/ChildrensmuseumEaston/images/BookswithBill.jpg\""
                    + "border=\"0\" /></a><br />Poor Bill, couldn't make it to the Museum's <span" + "class=\"blsp-spelling-corrected\" id=\"SPELLING_ERROR_0\">story time</span>"
                    + "today, he was so busy shoveling! Well, we sure missed you Bill! So since" + "ou were busy moving snow we read books about snow. We found a clue in one"
                    + "book which revealed a snowplow at the end of the story - we wish it had" + "driven to your driveway Bill. We also read a story which shared fourteen"
                    + "<em>Names For Snow. </em>We'll catch up with you next week....wonder which" + "hat Bill will wear?<br />Jane";

            Policy mySpacePolicy = GetPolicy("antisamy-myspace.xml");
            AntiySamyResult cr = _sut.Scan(dirty, mySpacePolicy);
            cr.CleanHtml.Should().NotBeNull();

            Policy ebayPolicy = GetPolicy("antisamy-ebay.xml");
            cr = _sut.Scan(dirty, ebayPolicy);
            cr.CleanHtml.Should().NotBeNull();

            Policy slashdotPolicy = GetPolicy("antisamy-slashdot.xml");
            cr = _sut.Scan(dirty, slashdotPolicy);
            cr.CleanHtml.Should().NotBeNull();
        }

        [Fact]
        public void issue38()
        {

            /* issue #38 - color problem/color combinations */
            var s = "<font color=\"#fff\">Test</font>";
            var expected = "<font color=\"#fff\">Test</font>";
            assertEquals(_sut.Scan(s, TestPolicy).CleanHtml, expected);

            //Not supported 
            //s = "<div style=\"color: #fff\">Test 3 letter code</div>";
            //expected = "<div style=\"color: rgb(255,255,255);\">Test 3 letter code</div>";
            //assertEquals(_sut.Scan(s, policy).CleanHtml, expected);

            s = "<font color=\"red\">Test</font>";
            expected = "<font color=\"red\">Test</font>";
            assertEquals(_sut.Scan(s, TestPolicy).CleanHtml, expected);

            s = "<font color=\"neonpink\">Test</font>";
            expected = "<font>Test</font>";
            assertEquals(_sut.Scan(s, TestPolicy).CleanHtml, expected);

            s = "<font color=\"#0000\">Test</font>";
            expected = "<font>Test</font>";
            assertEquals(_sut.Scan(s, TestPolicy).CleanHtml, expected);

            s = "<div style=\"color: #0000\">Test</div>";
            expected = "<div>Test</div>";
            assertEquals(_sut.Scan(s, TestPolicy).CleanHtml, expected);

            s = "<font color=\"#000000\">Test</font>";
            expected = "<font color=\"#000000\">Test</font>";
            assertEquals(_sut.Scan(s, TestPolicy).CleanHtml, expected);

            //Not supported 
            //s = "<div style=\"color: #000000\">Test</div>";
            //expected = "<div style=\"color: rgb(0,0,0);\">Test</div>";
            //assertEquals(_sut.Scan(s, policy).CleanHtml, expected);

            s = "<b><u>foo<style><script>alert(1)</script></style>@import 'x';</u>bar";
            _sut.Scan(s, TestPolicy);
        }

        [Fact]
        public void issue40()
        {
            /* issue #40 - handling <style> media attributes right */

            var s = "<style media=\"print, projection, screen\"> P { margin: 1em; }</style>";
            //Policy revised = policy.cloneWithDirective(Policy.PRESERVE_SPACE, "true");

            AntiySamyResult result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Contains("print, projection, screen").Should().BeTrue();

        }

        private void assertEquals(string actual, string expected)
        {
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void issue41()
        {
            /* issue #41 - comment handling */
            // comments will be removed by default
            _sut.Scan("text <!-- comment -->", TestPolicy).CleanHtml.Should().BeEquivalentTo("text ");

            //Policy revised2 = policy.cloneWithDirective(Policy.PRESERVE_COMMENTS, "true").cloneWithDirective(Policy.PRESERVE_SPACE, "true").cloneWithDirective(Policy.FORMAT_OUTPUT, "false");

            ///*
            //* These make sure the regular comments are kept alive and that
            //* conditional comments are ripped out.
            //*/
            //assertEquals("<div>text <!-- comment --></div>", as.scan("<div>text <!-- comment --></div>", revised2, AntiSamy.DOM).getCleanHTML());
            //assertEquals("<div>text <!-- comment --></div>", as.scan("<div>text <!--[if IE]> comment <[endif]--></div>", revised2, AntiSamy.DOM).getCleanHTML());

            ///*
            //* Check to see how nested conditional comments are handled. This is
            //* not very clean but the main goal is to avoid any tags. Not sure
            //* on encodings allowed in comments.
            //*/
            string input = "<div>text <!--[if IE]> <!--[if gte 6]> comment <[endif]--><[endif]--></div>";
            string expected = "<div>text &lt;[endif]--&gt;</div>";
            _sut.Scan(input, TestPolicy).CleanHtml.Should().BeEquivalentTo(expected);

            /*
            * Regular comment nested inside conditional comment. Test makes
            * sure
            */
            _sut.Scan("<div>text <!--[if IE]> <!-- IE specific --> comment <[endif]--></div>", TestPolicy).CleanHtml
                .Should().BeEquivalentTo("<div>text  comment &lt;[endif]--&gt;</div>");

            ///*
            //* These play with whitespace and have invalid comment syntax.
            //*/
            //assertEquals("<div>text <!-- \ncomment --></div>", as.scan("<div>text <!-- [ if lte 6 ]>\ncomment <[ endif\n]--></div>", revised2, AntiSamy.DOM).getCleanHTML());
            //assertEquals("<div>text  comment </div>", as.scan("<div>text <![if !IE]> comment <![endif]></div>", revised2, AntiSamy.DOM).getCleanHTML());
            //assertEquals("<div>text  comment </div>", as.scan("<div>text <![ if !IE]> comment <![endif]></div>", revised2, AntiSamy.DOM).getCleanHTML());

            var attack = "[if lte 8]<script>";
            var spacer = "<![if IE]>";

            var sb = new StringBuilder();

            sb.Append("<div>text<!");

            for (var i = 0; i < attack.Length; i++)
            {
                sb.Append(attack[i]);
                sb.Append(spacer);
            }

            sb.Append("<![endif]>");

            string s = sb.ToString();


            _sut.Scan(s, TestPolicy).CleanHtml.Contains("<script").Should().BeFalse();
        }

        [Fact]
        public void issue44()
        {
            /*
           * issue #44 - childless nodes of non-allowed elements won't cause an
           * error
           */
            string s = "<iframe src='http://foo.com/'></iframe>" + "<script src=''></script>" + "<link href='/foo.css'>";
            _sut.Scan(s, TestPolicy);
            _sut.Scan(s, TestPolicy).ErrorMessages.Count().Should().Be(3);

        }

        [Fact]
        public void issue51()
        {
            /* issue #51 - offsite urls with () are found to be invalid */
            var s = "<a href='http://subdomain.domain/(S(ke0lpq54bw0fvp53a10e1a45))/MyPage.aspx'>test</a>";
            AntiySamyResult result = _sut.Scan(s, TestPolicy);

            result.ErrorMessages.Count().Should().Be(0);
        }

        [Fact]
        public void isssue56()
        {
            /* issue #56 - unnecessary spaces */

            var s = "<SPAN style='font-weight: bold;'>Hello World!</SPAN>";
            var expected = "<span style='font-weight: bold'>Hello World!</span>";

            AntiySamyResult result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void issue58()
        {
            /* issue #58 - input not in list of allowed-to-be-empty tags */
            var s = "tgdan <input/> g  h";
            AntiySamyResult result = _sut.Scan(s, TestPolicy);
            result.ErrorMessages.Count().Should().Be(0);
        }

        [Fact]
        public void issue61()
        {
            /* issue #61 - input has newline appended if ends with an accepted tag */
            var dirtyInput = "blah <b>blah</b>.";
            //Format output not supported
            //Policy revised = policy.cloneWithDirective(Policy.FORMAT_OUTPUT, "false");
            AntiySamyResult result = _sut.Scan(dirtyInput, TestPolicy);
            result.CleanHtml.Should().BeEquivalentTo(dirtyInput);
        }

        [Fact]
        public void issue69()
        {
            /* issue #69 - char attribute should allow single char or entity ref */

            string s = "<table><tr><td char='.'>test</td></tr></table>";
            AntiySamyResult result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Contains("char").Should().BeTrue();

            s = "<table><tr><td char='..'>test</td></tr></table>";
            result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Contains("char").Should().BeFalse();

            s = "<table><tr><td char='&quot;'>test</td></tr></table>";
            result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Contains("char").Should().BeTrue();

            s = "<table><tr><td char='&quot;a'>test</td></tr></table>";
            result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Contains("char").Should().BeFalse();

            s = "<table><tr><td char='&quot;&amp;'>test</td></tr></table>";
            result = _sut.Scan(s, TestPolicy);
            result.CleanHtml.Contains("char").Should().BeFalse();
        }

        [Fact(Skip = "CData section is not supported and will be removed by default")]
        public void CDATAByPass()
        {
            String malInput = "<![CDATA[]><script>alert(1)</script>]]>";
            AntiySamyResult result = _sut.Scan(malInput, TestPolicy);
            result.ErrorMessages.Should().NotBeEmpty();
            result.CleanHtml.Should().Contain("&lt;script");
            result.CleanHtml.Should().NotContain("<script");
        }

        [Fact]
        public void literalLists()
        {

            /* this test is for confirming literal-lists work as
           * advertised. it turned out to be an invalid / non-
           * reproducible bug report but the test seemed useful
           * enough to keep.
           */
            var malInput = "hello<p align='invalid'>world</p>";

            AntiySamyResult result = _sut.Scan(malInput, TestPolicy);
            result.CleanHtml.Contains("invalid").Should().BeFalse();
            result.ErrorMessages.Count().Should().Be(1);

            var goodInput = "hello<p align='left'>world</p>";
            _sut.Scan(goodInput, TestPolicy).CleanHtml.Contains("left").Should().BeTrue();
        }
    }
}
