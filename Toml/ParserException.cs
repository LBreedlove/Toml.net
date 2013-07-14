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
    public class ParserException : ApplicationException
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
        /// Initializes a new instance of the ParserException class.
        /// </summary>
        /// <param name="lineNumber">The line number the parser error occurred on.</param>
        /// <param name="position">The position in the line the error was detected at.</param>
        /// <param name="currentLine">The text of the line the error occurred on.</param>
        /// <param name="message">The parser error message.</param>
        public ParserException(int lineNumber, int position, string currentLine, string message)
            : base(message)
        {
            this.LineNumber = lineNumber;
            this.Position = position;
            this.Context = currentLine;
        }

        /// <summary>
        /// Gets the line number the error occurred on.
        /// </summary>
        public int LineNumber { get; private set; }

        /// <summary>
        /// Gets the position of the error on the line.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Gets the context of the error.
        /// </summary>
        public string Context { get; private set; }
    }
}
