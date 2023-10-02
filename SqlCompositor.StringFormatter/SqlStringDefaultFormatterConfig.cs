using System.Text.RegularExpressions;

namespace SqlCompositor.StringFormatter
{
    /// <summary>
    /// Optimized version that uses 
    /// </summary>
    public partial class SqlStringDefaultFormatterConfig : SqlStringFormatterConfig
    {
        public SqlStringDefaultFormatterConfig() : base(ColumnNameRegex(), SqlTypeRegex(), JsonPathRegex(), new OutputConfig(WhitespaceType.Spaces, indentSize: 4))
        {
        }

        public static SqlStringDefaultFormatterConfig Instance { get; } = new();

        /// <summary>
        /// alphanumeric type followed by an optional length specifier (or max)
        /// covers all the cases we need to care about and is safe from injection
        /// </summary>
        [GeneratedRegex("^[a-z0-9]+(?:\\((?:\\d+|max)\\))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex SqlTypeRegex();

        /// <summary>
        /// Currently this covers all the required column syntax.
        /// 
        /// We use \p{L} to cover all unicode letters (Lu/Ll/Lt/Lm/Lo)
        /// https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions#SupportedUnicodeGeneralCategories
        /// </summary>
        [GeneratedRegex("^#?(?:[\\p{L}0-9()\\-/&_$:.]|\\s)*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex ColumnNameRegex();

        /// <summary>
        /// Is just the column syntax + [] and "
        /// </summary>
        [GeneratedRegex("^(?:[a-zA-Z0-9\\-_$:./&\\[\\]\"]|\\s)*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex JsonPathRegex();
    }
}
