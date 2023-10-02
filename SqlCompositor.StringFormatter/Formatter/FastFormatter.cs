using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCompositor.StringFormatter.Formatter
{
    /// <summary>
    /// This attempts to format SQL to something that is approximately readable.
    /// 
    /// This is to optimise the readability of the outputs of SqlStringFormatter
    /// since the use of nested queries and multiple lines of SqlIfs() can result
    /// in large holes full of whitespace.
    /// 
    /// Thus this is basically a whitespace formatter.
    /// </summary>
    public class FastFormatter
    {
    }
}
