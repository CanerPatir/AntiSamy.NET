namespace AntiSamy
{
    public class Consts
    {
        public const string ANY_NORMAL_WHITESPACES = "(\\s)*";
        public const string OPEN_ATTRIBUTE = "(";
        public const string ATTRIBUTE_DIVIDER = "|";
        public const string CLOSE_ATTRIBUTE = ")";

        public class OnInvalidActions
        {
            public const string REMOVE_TAG = "removeTag";
            public const string REMOVE_ATTRIBUTE = "removeAttribute";
            public const string FILTER_TAG = "filterTag";
        }

        public class  TagActions
        {
            public const string FILTER = "filter"; // remove tags but keep content 
            public const string VALIDATE = "validate"; // keep content as long as it passes rules
            public const string REMOVE = "remove"; // remove tag and contents
        }
    }
}
