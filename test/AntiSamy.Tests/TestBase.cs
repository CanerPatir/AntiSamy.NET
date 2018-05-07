using System.IO;

namespace AntiSamy.Tests
{
    public abstract class TestBase
    {
        private const string DefaultAntiSamyFile = "antisamy.xml";
        protected readonly Policy TestPolicy;

        protected TestBase()
        {
            TestPolicy = GetPolicy(DefaultAntiSamyFile);
        }

        protected Policy GetPolicy(string fileName)
        {
            string currentDir = Directory.GetCurrentDirectory();
            return Policy.FromFile(Path.Combine(currentDir, $@"resources\{fileName}"));
        }
    }
}
