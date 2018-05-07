namespace AntiSamy
{
    public class AntiSamy
    {
        public virtual AntiySamyResult Scan(string taintedHtml, string filename)
        {
            Policy policy = Policy.FromFile(filename);

            var antiSamy = new AntiSamyDomScanner(policy);

            return antiSamy.Scan(taintedHtml);
        }

        public virtual AntiySamyResult Scan(string taintedHtml, Policy policy)
        {
            var antiSamy = new AntiSamyDomScanner(policy);

            return antiSamy.Scan(taintedHtml);
        }

    }
}
