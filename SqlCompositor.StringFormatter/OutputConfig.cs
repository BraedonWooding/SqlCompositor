namespace SqlCompositor.StringFormatter
{
    public enum WhitespaceType
    {
        Spaces,
        Tabs,
    }

    public class OutputConfig
    {
        /// <summary>
        /// What characters to use for indents.
        /// </summary>
        public WhitespaceType WhitespaceType { get; }
        
        /// <summary>
        /// How many tabs or spaces to use per indent level.
        /// </summary>
        public int IndentSize { get; }

        public OutputConfig(WhitespaceType whitespaceType = WhitespaceType.Spaces, int indentSize = 4)
        {
            WhitespaceType = whitespaceType;
            IndentSize = indentSize;
        }
    }
}
