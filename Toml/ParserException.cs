using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    /// <summary>
    /// Represents a Parser Exception.
    /// </summary>
    class ParserException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the ParserException class.
        /// </summary>
        /// <param name="lineNumber">The line number the parser error occurred on.</param>
        /// <param name="message">The parser error message.</param>
        public ParserException(int lineNumber, string message)
            : base(message)
        {
            this.LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the line number the error occurred on.
        /// </summary>
        public int LineNumber { get; private set; }
    }
}
