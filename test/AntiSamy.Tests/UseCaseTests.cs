using FluentAssertions;

using Xunit;

namespace AntiSamy.Tests
{
    public class UseCaseTests : TestBase
    {
        protected override string DefaultAntiSamyFile => "antisamy1.xml";

        [Fact]
        public void invalid_img_urls_should_be_filtered()
        {
            var scanner = new AntiSamy();

            /*
             * remove non-allowed image srcs
             */

            var input = @"<div>
                                <img src='mysite.com/image.jpg' /> <!-- to be allowed --!>
                                Some description 
                                <img src='hackers.com/xss.js' />
                             </div>";

            AntiySamyResult result = scanner.Scan(input, TestPolicy);

            // safe - allowed url pattern in the antisamy1.xml
            result.CleanHtml.Should().Contain("Some description");
            result.CleanHtml.Should().Contain("<div");
            result.CleanHtml.Should().Contain("mysite.com/image.jpg");

            // non safe
            result.CleanHtml.Should().NotContain("hackers.com/xss.js");
        }

        [Fact]
        public void invalid_tags_should_be_removed()
        {
            var scanner = new AntiSamy();

            /*
             * remove iframe, object, embed, frame, frameset
             */

            var input = @"<div>
                                Some description 
                                <iframe src='hackers.com/xss' />
                                <object data='hackers.com/xss' />
                                <embed />
                                <frame />
                                <frameset />
                             </div>";

            AntiySamyResult result = scanner.Scan(input, TestPolicy);

            //safe
            result.CleanHtml.Should().Contain("<div");
            result.CleanHtml.Should().Contain("Some description");

            // non safe
            result.CleanHtml.Should().NotContain("<iframe");
            result.CleanHtml.Should().NotContain("<object");
            result.CleanHtml.Should().NotContain("<embed");
            result.CleanHtml.Should().NotContain("<frame");
            result.CleanHtml.Should().NotContain("<frameset");
        }

        [Fact]
        public void invalid_a_hrefs_should_be_filtered()
        {
            var scanner = new AntiSamy();

            /*
             * remove non-allowed hrefs
             */

            var input = @"<div>
                                <a href='mysite.com/image.jpg' /> <!-- to be allowed --!>
                                <a href='mysite.com/some_relative_path' /> <!-- to be allowed --!>
                                <a href='mysite.com/some_relative_path/level2' /> <!-- to be allowed --!>
                                Some description 
                                <a href='hackers.com/xss.js' />
                                <a href='abc.com' />
                                another description
                             </div>";

            AntiySamyResult result = scanner.Scan(input, TestPolicy);

            // safe - allowed url pattern in the antisamy1.xml
            result.CleanHtml.Should().Contain("<div");
            result.CleanHtml.Should().Contain("Some description");
            result.CleanHtml.Should().Contain("another description");
            result.CleanHtml.Should().Contain("mysite.com/image.jpg");
            result.CleanHtml.Should().Contain("mysite.com/some_relative_path");
            result.CleanHtml.Should().Contain("mysite.com/some_relative_path/level2");

            // non safe
            result.CleanHtml.Should().NotContain("hackers.com/xss.js");
            result.CleanHtml.Should().NotContain("abc.com");

        }

        [Fact]
        public void script_references_should_be_removed_by_default()
        {
            var scanner = new AntiSamy();

            /*
             * remove non-allowed hrefs
             */

            var input = @"<script type='text/javascript' src='hackers.com/xss.js' />
                          <script>alert('XSS !!!');</script>
                          <div>
                                Some description                                
                                <script type='text/javascript' src='hackers.com/xss.js' />
                          </div>";

            AntiySamyResult result = scanner.Scan(input, TestPolicy);

            //safe
            result.CleanHtml.Should().Contain("<div");
            result.CleanHtml.Should().Contain("Some description");

            // non safe
            result.CleanHtml.Should().NotContain("<script");

        }

    }
}
