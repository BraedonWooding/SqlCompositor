using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using SqlCompositor.Core.Model;
using SqlCompositor.Core.RawFormatter;

namespace SqlCompositor.StringFormatter
{
    public partial class SqlStringFormatter
    {
        private SqlStringFormatter(string sqlCommand, SqlParameters parameters, TrackingInfo info)
        {
            SqlCommand = sqlCommand;
            Parameters = parameters;
            Info = info;
        }

        public string SqlCommand { get; set; }
        public SqlParameters Parameters { get; set; }
        public TrackingInfo Info { get; private set; }

        public static SqlStringFormatter Sql(FormattableString str, List<object>? initial = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int? lineNumber = null)
        {
            var parameters = new SqlParameters(initial);
            var resultSql = ParseSqlStatement(str, parameters);

            return new SqlStringFormatter(resultSql, parameters, new TrackingInfo(memberName, lineNumber));
        }

        private static string ParseSqlStatement(FormattableString str, SqlParameters sqlParameters)
        {
            try
            {
                var formatter = new SqlFormatProvider(sqlParameters);
                return FormatSql(str.ToString(formatter));
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to format\n{FormatSql(str.Format)}\n{e.Message}");
            }
        }

        private static string FormatSql(string sql)
        {
            // just re-indents everything to 4 spaces
            // get rid of the awful \r\n for just \n
            sql = sql.Replace(Environment.NewLine, "\n").TrimEnd(' ', '\n');

            var lines = sql.Split('\n').SkipWhile(string.IsNullOrWhiteSpace).ToArray();
            if (lines.Length == 0) return "";
            else if (lines.Length == 1) return lines[0];

            var indentScale = (double)lines[0].TakeWhile(char.IsWhiteSpace).Count();

            // re-indent based on scale i.e. if it was 8 -> 16 -> 24 before now it's 4 -> 8 -> 12
            return string.Join('\n', lines
                .Where(line => line != null)
                .Select(line =>
                {
                    var indent = line.TakeWhile(char.IsWhiteSpace).Count();
                    if (indent == 0 || indentScale < 4) indent = 4;
                    else indent = (int)Math.Ceiling(indent / indentScale) * 4;

                    return new string(' ', indent) + line.TrimStart();
                }));
        }

        public override string ToString()
        {
            string paramTypes = "N'";
            string values = "";
            for (int i = 0; i < Parameters.Parameters.Count; i++)
            {
                var p = Parameters.Parameters[i];
                // we only support strings/ints/datetimes for now
                if (p == null)
                {
                    paramTypes += $"{(i > 0 ? ", " : "")}@p{i} BIT";
                    values += $", @p{i}=NULL";
                }
                else if (p is string strVal)
                {
                    paramTypes += $"{(i > 0 ? ", " : "")}@p{i} NVARCHAR({strVal.Length})";
                    values += $", @p{i}=N'{strVal}'";
                }
                else if (p is DateTime dtVal)
                {
                    // TODO: We should support datetime offsets & support time component for datetime.
                    //       Store everything in ISO
                    var dateStr = dtVal.ToString("yyyy-MM-dd");
                    paramTypes += $"{(i > 0 ? ", " : "")}@p{i} DATETIME2";
                    values += $", @p{i}='{dateStr}'";
                }
                else if (p is int intVal)
                {
                    paramTypes += $"{(i > 0 ? ", " : "")}@p{i} INT";
                    values += $", @p{i}={intVal}";
                }
                else
                {
                    var convertedValue = p.ToString() ?? "";
                    paramTypes += $"{(i > 0 ? ", " : "")}@p{i} NVARCHAR({convertedValue.Length})";
                    values += $", @p{i}={convertedValue}";
                }
            }
            paramTypes += "'";

            // we want to return a string that is executable in SSMS and is roughly how it'll actually be executed.
            return $"DECLARE @SQL NVARCHAR(MAX) = '{SqlFormattingHelper.QuoteIdentifier(SqlCommand, Enums.SqlQuoteType.SingleQuote)}'\n" +
                $"EXEC sp_executesql @SQL, {paramTypes}{values}";
        }

        public static implicit operator SqlStringFormatter(FormattableString input) => Sql(input);

        /// <summary>
        /// Given a condition lazily-evaluate one of two branches useful in cases where you'll be using nullables values.
        /// 
        /// For example the following could be used to optimise an OFFSET/TAKE in the case where we only have a take.
        /// <code>
        ///     SqlIf(limits != null &amp;&amp; limits.Skip ?? 0 == 0, () =&gt; $"TOP({limit.Take:int})")
        /// </code>
        /// </summary>
        public static FormattableString SqlIf(bool condition, Func<FormattableString> ifTrue, Func<FormattableString>? ifFalse = null)
        {
            return condition ? ifTrue() : (ifFalse?.Invoke() ?? $"");
        }

        public static bool IsValidColumnName(string columnName)
        {
            return ColumnRegex().IsMatch(columnName);
        }

        public static bool IsValidJsonPath(string path)
        {
            return JsonPathRegex().IsMatch(path);
        }

        /// <summary>
        /// Cleans up an id by replacing 
        /// </summary>
        public static bool IsValidId(string id)
        {
            return ColumnRegex().IsMatch(id);
        }

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
        private static partial Regex ColumnRegex();

        /// <summary>
        /// Is just the column syntax + [] and "
        /// </summary>
        [GeneratedRegex("^(?:[a-zA-Z0-9\\-_$:./&\\[\\]\"]|\\s)*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex JsonPathRegex();
    }
}