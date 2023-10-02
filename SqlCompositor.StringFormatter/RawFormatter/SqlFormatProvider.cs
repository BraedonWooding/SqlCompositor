using SqlCompositor.Core.Enums;
using SqlCompositor.Core.Model;
using SqlCompositor.Core.RawFormatter;

namespace SqlCompositor.Core
{
    public partial class SqlStringFormatter
    {
        private partial class SqlFormatProvider : IFormatProvider
        {
            private readonly SqlFormatter _formatter;

            public SqlFormatProvider(SqlParameters parameters)
            {
                _formatter = new SqlFormatter();
                Parameters = parameters;
            }

            public SqlParameters Parameters { get; }

            public object? GetFormat(Type? formatType)
            {
                // we handle all formatting of *everything*
                if (formatType == typeof(ICustomFormatter)) return _formatter;
                return null;
            }

            private partial class SqlFormatter : ICustomFormatter
            {
                // not necessarily a super accurate way of tracking indexes
                // but since our format strings are manipulated in such a way as to be ordered when evaluated
                // it should result in this being accurate (since this class has a short lifetime and isn't shared even in inner evaluations)
                private int index = -1;

                public string Format(string? format, object? arg, IFormatProvider? formatProvider)
                {
                    if (formatProvider is not SqlFormatProvider sqlFormatProvider)
                    {
                        throw new ArgumentException("Expected formatProvider to be of type SqlFormatProvider", nameof(formatProvider));
                    }

                    var sqlParameters = sqlFormatProvider.Parameters;
                    index++;

                    // This is fine, even without a valid format
                    // just too annoying to null coalesce all of arg below without missing something.
                    if (arg == null && format != "value") return "";
                    var stringArg = arg is FormattableString formatString ? formatString.Format : arg?.ToString();



                    if (format == "inline" && arg is FormattableString str)
                    {
                        try
                        {
                            var formatter = new SqlFormatProvider(sqlParameters);
                            return str.ToString(formatter);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Where {{{index}:{format}}} = \n{FormatSql(str.Format)}\n{e.Message}");
                        }
                    }
                    else if (format == "inline")
                    {
                        throw new FormatException($"{{{index}:{format}}}: Can only use FormattableStrings (i.e. $\"...\") in :inline statements, it's of type {(arg?.GetType().Name ?? "<null>")} (value = {stringArg})");
                    }
                    else if (arg is FormattableString)
                    {
                        throw new FormatException($"{{{index}:{format}}}: Can only use FormattableStrings (i.e. $\"...\") in :inline statements, format is {format} instead of :inline");
                    }
                    else if (format == "value")
                    {
                        // parameterise
                        return sqlParameters.Value(arg);
                    }
                    else if (format == "int")
                    {
                        if (arg is not int i && !int.TryParse(stringArg, out i)) throw new FormatException($"{{{index}:{format}}}: Argument must be a valid integer {i} is not a valid integer");
                        return i.ToString();
                    }
                    else if (format == "datepart")
                    {
                        // We use _ to make it cleaner to read
                        if (arg is SqlDatePart op) return op.ToString();
                        else throw new ArgumentException("Expected argument to :datepart to be of type SqlDatePart");
                    }
                    else if (format == "jsonpath")
                    {
                        // this is our *only* protection, try to avoid using this and instead opt in for parameters where possible
                        // there are some cases where we can't (i.e. OPENJSON WITH statements)
                        if (!JsonPathRegex().IsMatch(stringArg)) throw new FormatException($"{{{index}:{format}}}: Potential SQL injection detected ({arg}), malicious characters found");
                        return SqlFormattingHelper.QuoteIdentifier(stringArg, SqlQuoteType.SingleQuote);
                    }
                    else if (format == "id")
                    {
                        // None of these characters would make sense in a variable name
                        if (!ColumnRegex().IsMatch(stringArg)) throw new FormatException($"{{{index}:{format}}}: Potential SQL injection detected ({arg}), malicious characters found");
                        return SqlFormattingHelper.QuoteIdentifier(stringArg, SqlQuoteType.Bracket);
                    }
                    else if (format == "type")
                    {
                        // Validation of type, no way to quote or wrap this so we do have to fall back to validation
                        if (arg != null && SqlTypeRegex().IsMatch(stringArg)) return stringArg;
                        else throw new FormatException($"{{{index}:{format}}}: Invalid SQL Type, doesn't match required regex: {arg ?? "<null>"}");
                    }
                    else if (format == "fqn")
                    {
                        // @TODO: Fully qualified name (fact table)
                        // Going to let it leak for now since we only have one use case cfg.FactTable
                        // And this is not particularly easy to solve.
                        return stringArg;
                    }
                    else if (format == "orderby")
                    {
                        if (arg is OrderByOperator op) return op.ToString();
                        else throw new ArgumentException("Expected argument to :orderby to be of type OrderByOperator");
                    }
                    else if (format == "unsafeString")
                    {
                        // this isn't inheritely that unsafe, it's just more unsafe than a {value}
                        return SqlFormattingHelper.QuoteIdentifier(stringArg, SqlQuoteType.SingleQuote);
                    }

                    if (string.IsNullOrWhiteSpace(format))
                    {
                        throw new FormatException($"{{{index}:{format}}}: Missing format for value \"{stringArg}\"");
                    }
                    else
                    {
                        throw new FormatException($"{{{index}:{format}}}: Invalid format {format} for value \"{stringArg}\"");
                    }
                }
            }
        }
    }
}