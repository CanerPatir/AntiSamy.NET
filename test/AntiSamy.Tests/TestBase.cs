using System.IO;

namespace AntiSamy.Tests
{
    public abstract class TestBase
    {
        protected readonly Policy TestPolicy;

        protected  virtual string DefaultAntiSamyFile => "antisamy.xml";

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
