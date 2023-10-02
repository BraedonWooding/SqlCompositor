using System.Runtime.CompilerServices;
using System.Text;
using SqlCompositor.Core.Enums;

namespace SqlCompositor.Core.RawFormatter
{
    public static class SqlFormattingHelper
    {
        public static string QuoteIdentifier(string arg, SqlQuoteType quote) => quote switch
        {
            SqlQuoteType.Bracket => QuoteIdentifier(arg, "[", "]"),
            SqlQuoteType.SingleQuote => QuoteIdentifier(arg, "'", "'"),
            _ => throw new NotImplementedException($"{quote} is not yet implemented"),
        };

        private static string QuoteIdentifier(string arg, string prefix, string suffix)
        {
            // @NOTE: Due to SQL quoting rules we only duplicate the suffix not the prefix
            //        so `[[A]` is valid and is simply just [A (though that alone is not valid weirdly enough)
            //        You can see this behaviour here: https://github.com/microsoft/referencesource/blob/dae14279dd0672adead5de00ac8f117dcf74c184/System.Data/System/Data/Common/AdapterUtil.cs#L1982
            return $"{prefix}{arg.Replace(suffix, suffix + suffix)}{suffix}";
        }

        /// <summary>
        /// Performs a map-reduce on a stream to produce a SQL formatted command.
        /// 
        /// You pass in a mapping function to dictate the conversion to a formattable string that follows the restrictions
        /// on SQL formatted strings (i.e. you always need a format, and it needs to be an appropriate format for the value).
        /// 
        /// You can then pass an optional join argument (default "").
        /// </summary>
        public static FormattableString ConcatFormat<T>(this IEnumerable<T> stream, Func<T, FormattableString> map, string join)
        {
            return stream.Select(map).Join(join);
        }

        /// <summary>
        /// Simpler form of <see cref="ConcatFormat{T}(IEnumerable{T}, Func{T, FormattableString}, string)"/> that avoids the map since the argument
        /// is already formattable strings.
        /// 
        /// You may need to explicitly cast them i.e. (FormattableString)$"" to avoid type errors.
        /// </summary>
        public static FormattableString Join(this IEnumerable<FormattableString> stream, string join)
        {
            if (!stream.Any()) return $"";

            // The use of FormattableString avoids concreting/'formatting' this stream prior to having the context
            // of the outer expressions parameters.
            var textJoined = new StringBuilder();
            var args = new List<object?>();
            int currentArgCount = 0;

            // we have to update all the formats since the joining here would cause it to re-use the args from the first one.
            // i.e. {0} {0} rather than {0} {1}
            foreach (var item in stream)
            {
                int argCount = item.ArgumentCount;
                args.AddRange(item.GetArguments());
                if (textJoined.Length > 0) textJoined.Append(join);
                var format = item.Format;

                if (currentArgCount > 0)
                {
                    // we have to cycle through backwards because if we go forwards we'll increment all format indices
                    // i.e. if we have 0, 1, 2 and we are incrementing by 1 if we go forwards it'll go
                    // 1. replace("{0:id}, {1:id}, {2:id}", "{0:", "{1:")
                    // 2. replace("{1:id}, {1:id}, {2:id}", "{1:", "{2:")
                    // 3. replace("{2:id}, {2:id}, {2:id}", "{2:", "{3:")
                    // 4.         "{3:id}, {3:id}, {3:id}"
                    // And if we go backwards we won't ever get conflicts (since we are always *increasing the indices*)
                    // 1. replace("{0:id}, {1:id}, {2:id}", "{2:", "{3:")
                    // 2. replace("{0:id}, {1:id}, {3:id}", "{1:", "{2:")
                    // 3. replace("{0:id}, {2:id}, {3:id}", "{0:", "{1:")
                    // 4.         "{1:id}, {2:id}, {3:id}"
                    for (var i = argCount - 1; i >= 0; i--)
                    {
                        // @NOTE: This is a bit naive, we may end up having some issues but I don't see any major issues with it in our current system.
                        format = format.Replace($"{{{i}:", $"{{{i + currentArgCount}:");
                    }
                }

                currentArgCount += argCount;
                textJoined.Append(format.Trim(' ', '\n', '\r'));
            }

            return FormattableStringFactory.Create(textJoined.ToString(), args.ToArray());
        }
    }
}