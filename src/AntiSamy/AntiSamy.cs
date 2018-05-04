namespace AntiSamy
{
    public class AntiSamy
    {
        public string InputEncoding { get; } = AntiSamyDomScanner.DefaultEncodingAlgorithm;

        public string OutputEncoding { get; } = AntiSamyDomScanner.DefaultEncodingAlgorithm;

        public virtual AntiySamyResult Scan(string taintedHtml, string filename)
        {
            Policy policy = Policy.FromFile(filename);

            var antiSamy = new AntiSamyDomScanner(policy);

            return antiSamy.Scan(taintedHtml, InputEncoding, OutputEncoding);
        }

        public virtual AntiySamyResult Scan(string taintedHtml, Policy policy)
        {
            var antiSamy = new AntiSamyDomScanner(policy);

            return antiSamy.Scan(taintedHtml, InputEncoding, OutputEncoding);
        }

    }
}
