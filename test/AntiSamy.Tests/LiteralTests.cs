using FluentAssertions;
using System.Linq;
using Xunit;

namespace AntiSamy.Tests
{
    public class LiteralTests : TestBase
    {

        [Fact]
        public void Test_dom_good_result()
        {
            var html = "<div align=\"right\">html</div>";

            AntiySamyResult result = new AntiSamy().Scan(html, TestPolicy);

            result.ErrorMessages.Count().Should().Be(0);
        }

        [Fact]
        public void TestDomBadResult()
        {
            var badHtml = "<div align=\"foo\">badhtml</div>";

            AntiySamyResult result = new AntiSamy().Scan(badHtml, TestPolicy);

            result.ErrorMessages.Count().Should().BeGreaterThan(0);
        }
    }
}
