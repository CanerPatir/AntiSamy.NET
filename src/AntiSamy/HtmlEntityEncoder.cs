using System.Text;

namespace AntiSamy
{
    internal class HtmlEntityEncoder
    {
        public static string HtmlEntityEncode(string value)
        {
            var sb = new StringBuilder();
            if (value == null)
            {
                return null;
            }

            for (var i = 0; i < value.Length; i++)
            {
                char ch = value[i];

                switch (ch)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    default:
                        if (char.IsWhiteSpace(ch))
                        {
                            sb.Append(ch);
                        }
                        else if (char.IsLetterOrDigit(ch))
                        {
                            sb.Append(ch);
                        }
                        else if (ch >= 20 && ch <= 126)
                        {
                            sb.Append("&#" + (int)ch + ";");
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
