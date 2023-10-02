using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SqlCompositor.StringFormatter
{
    /// <summary>
    /// Configuration for <see cref="SqlStringFormatter"/>
    /// 
    /// The default value is <see cref="SqlStringDefaultFormatterConfig"/>.
    /// </summary>
    public partial class SqlStringFormatterConfig
    {
        public SqlStringFormatterConfig(Regex columnName, Regex sqlType, Regex jsonPath, OutputConfig output)
        {
            ColumnName = columnName;
            SqlType = sqlType;
            JsonPath = jsonPath;
            Output = output;
        }

        public Regex ColumnName { get; }
        public Regex SqlType { get; }
        public Regex JsonPath { get; }

        public OutputConfig Output { get; }

        /// <summary>
        /// Default formatter.  Be careful overriding this but it is useful if you wish to
        /// </summary>
        public static SqlStringFormatterConfig Default { get; set; } = SqlStringDefaultFormatterConfig.Instance;
    }
}
