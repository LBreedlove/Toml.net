using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{

    /// <summary>
    /// Class used to break the document up into Entry instances.
    /// </summary>
    internal class Parser
    {
        public enum Mode
        {
            Scanning = 0,
            ReadingValueName,
            SearchingForValueSeparator,
            SearchingForArraySeparator,
            ReadingArrayEnd,
            SearchingForValue,
            ReadingValue,
            ReadingStringValue,
            ReadingMultiLineStringValue
        }

        /// <summary>
        /// The errors used when throwing parser exceptions
        /// </summary>
        public static class Errors
        {
            public static readonly string InvalidValue = "Invalid Value";
            public static readonly string InvalidIdentifierCharacter = "Invalid Identifier Character";
            public static readonly string InvalidEscapeCharacter = "Invalid Escape Character Detected";
            public static readonly string IncompleteToken = "Incomplete token on close";

            public static readonly string UnexpectedArrayTerminator = "Unexpected Array Terminator";
            public static readonly string UnexpectedNewLineInString = "Unexpected Newline in String Value";
            public static readonly string UnexpectedNewLineInKeyName = "Unexpected Newline in KeyName";
            public static readonly string UnexpectedCommentInValue = "Unexpected comment in value";
            
            public static readonly string KeyNameIsEmpty = "KeyName cannot be empty";
            public static readonly string IdentifierNameIsEmpty = "Identifier name cannot be empty";

            public static readonly string ExpectingIdentifier = "Unexpected character - Expecting Identifier";
            public static readonly string ExpectingAssignmentOperator = "Expected = before value";
            public static readonly string ExpectingArraySeparator = "Expected , before value";
        }

        /// <summary>
        /// The tokens that are used by the parser.
        /// </summary>
        internal class Tokens
        {
            public static readonly string GroupSeparator = ".";

            public static readonly char Comment = '#';
            public static readonly char Negative = '-';
            public static readonly char Decimal = '.';

            public static readonly char KeyStart = '[';
            public static readonly char KeySeparator = '.';
            public static readonly char KeyEnd = ']';

            public static readonly char ArrayStart = '[';
            public static readonly char ArraySeparator = ',';
            public static readonly char ArrayEnd = ']';

            public static readonly char ValueSeparator = '=';

            public static readonly char EscapeChar = '\\';
            public static readonly char QuoteStart = '\"';
            public static readonly char QuoteEnd = '\"';

            public static readonly char MultiLineQuoteStart = '\"';
            public static readonly char MultiLineQuoteEnd = '\"';

            public static readonly string MultiLineQuoteStartToken = "\"\"\"";
            public static readonly string MultiLineQuoteEndToken = "\"\"\"";

            public static readonly string EmptyStringToken = "\"\"";
        }

        /// <summary>
        /// The set of characters that are considered Whitespace.
        /// </summary>
        public static readonly char[] Whitespace =
        {
            ' ',
            '\t',
            '\r',
            '\n'
        };

        /// <summary>
        /// Class used to track the state of the parser, since the parser is static.
        /// </summary>
        private class State
        {
            #region Public Methods

            /// <summary>
            /// Creates a new Toml.Entry for the current value, in the current group.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public Entry CreateEntry(Toml.Entry.TomlType type)
            {
                if (type == Entry.TomlType.String)
                {
                    // get the escaped string value
                    this.CurrentValue = Parser.GetEscapedString(this.CurrentValue);
                }

                if (type == Toml.Entry.TomlType.Array)
                {
                    Array array;
                    if (this.CurrentArray != null)
                    {
                        array = new Array(this.CurrentArray, this.LineNumber, this.Position);
                    }
                    else
                    {
                        array = new Array(this.CurrentGroupName, this.CurrentValueName, this.LineNumber, this.Position);
                    }

                    this.CurrentArray = array;
                    this.ArrayDepth++;

                    this.CurrentValue = string.Empty;
                    return array;
                }

                if (this.CurrentArray != null)
                {
                    var entry = new Entry(this.CurrentArray, this.CurrentGroupName, this.CurrentValue, this.LineNumber, this.Position, type);
                    this.CurrentArray.AddEntry(entry);

                    this.CurrentValue = string.Empty;
                    return entry;
                }
                else
                {
                    var entry = new Entry(this.CurrentGroupName, this.CurrentValueName, this.CurrentValue, this.LineNumber, this.Position, type);
                    this.CurrentValue = string.Empty;
                    return entry;
                }
            }

            /// <summary>
            /// Closes the current array.
            /// </summary>
            /// <returns></returns>
            public Array CloseArray()
            {
                if (this.ArrayDepth == 0)
                {
                    throw this.CreateError(Parser.Errors.UnexpectedArrayTerminator);
                }

                this.ArrayDepth--;
                var closedArray = this.CurrentArray;
                closedArray.UpdateSourceText();
                this.CurrentArray = this.CurrentArray.Parent;
                return (this.InArray ? null : closedArray);
            }

            public ParserException CreateError(int pos, string message)
            {
                return new ParserException(this.LineNumber, pos, this.CurrentLine, message);
            }

            public ParserException CreateError(string message)
            {
                return new ParserException(this.LineNumber, this.Position, this.CurrentLine, message);
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets the current line being used by the parser.
            /// </summary>
            public string CurrentLine { get; set; }

            /// <summary>
            /// Indicates whether or not the parser needs a new line to continue.
            /// </summary>
            public bool NeedsNewLine
            {
                get
                {
                    if (String.IsNullOrEmpty(this.CurrentLine))
                    {
                        return true;
                    }

                    if (this.CurrentLine.Length <= this.Position)
                    {
                        return true;
                    }

                    return false;
                }
            }

            /// <summary>
            /// The number of the line currently being parsed.
            /// </summary>
            public int LineNumber { get; set; }

            /// <summary>
            /// The current position in the line being parsed.
            /// </summary>
            public int Position { get; set; }

            /// <summary>
            /// The number of arrays deep we currently are.
            /// </summary>
            public int ArrayDepth { get; private set; }

            /// <summary>
            /// The array values are being added to, if any.
            /// </summary>
            public Array CurrentArray { get; private set; }

            /// <summary>
            /// Indicates whether or not we are currently parsing an array.
            /// </summary>
            public bool InArray { get { return this.ArrayDepth > 0; } }

            /// <summary>
            /// The name of the group tokens are currently being assigned to.
            /// </summary>
            public string CurrentGroupName { get; set; }

            /// <summary>
            /// The name of the value currently being parsed.
            /// </summary>
            public string CurrentValueName { get; set; }

            /// <summary>
            /// The value that has been parsed so far, for the current value.
            /// </summary>
            public string CurrentValue { get; set; }

            /// <summary>
            /// Gets or sets a value indicating what the parser is currently
            /// doing.
            /// </summary>
            public Mode Mode { get; set; }

            #endregion
        }

        /// <summary>
        /// Indicates whether or not the specified character is a whitespace char.
        /// </summary>
        /// <param name="value">The character value to test.</param>
        /// <returns></returns>
        public static bool IsWhitespace(char value)
        {
            return Parser.Whitespace.Any(ws => ws == value);
        }

        /// <summary>
        /// Indicates whether or not the character is valid to start an identifier
        /// </summary>
        /// <param name="value">The character value to test.</param>
        /// <returns>true if the character is a valid identifier start character, otherwise false.</returns>
        public static bool IsValidIdentifierStartChar(char value)
        {
            return !((!char.IsLetter(value)) && (value != '_'));
        }

        /// <summary>
        /// Indicates whether or not the character is valid to include in an identifier
        /// </summary>
        /// <param name="value">The character value to test.</param>
        /// <returns>true if the character is a valid identifier character, otherwise false.</returns>
        public static bool IsValidIdentifierChar(char value)
        {
            return !((!char.IsLetterOrDigit(value)) && (value != '_') && (value != '-'));            
        }

        /// <summary>
        /// Gets the escaped value of the character.
        /// </summary>
        private static char GetEscapedCharacter(int pos, char value, State state)
        {
            switch (value)
            {
                case ('0'):
                    return '\0';
                case ('n'):
                    return '\n';
                case ('r'):
                    return '\r';
                case ('t'):
                    return '\t';
                case ('\\'):
                    return '\\';
                case ('\"'):
                    return '\"';
            }

            throw state.CreateError(pos, string.Format("{0}: {1}", Parser.Errors.InvalidEscapeCharacter, value));
        }

        /// <summary>
        /// Gets the value of the string after processing any embedded escape characters.
        /// </summary>
        /// <param name="source">The string to get the value of.</param>
        /// <returns>The escaped value of the string.</returns>
        public static string GetEscapedString(string source)
        {
            // TODO: IMPLEMENT
            return source;
        }

        /// <summary>
        /// Parses the specified text as if it was encountered in a file.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns></returns>
        public static IEnumerable<Entry> ParseEntry(string text)
        {
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    State state = new State();

                    var entry = GetNextEntry(reader, state);
                    while (entry != null)
                    {
                        yield return entry;
                        entry = GetNextEntry(reader, state);
                    }

                    if (state.Mode != Mode.Scanning)
                    {
                        throw state.CreateError(Parser.Errors.IncompleteToken);
                    }
                }

                yield break;
            }
        }

        /// <summary>
        /// Parses the document from the named file, lazily.
        /// </summary>
        public static IEnumerable<Entry> Parse(String source)
        {
            using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    State state = new State();

                    var entry = GetNextEntry(reader, state);
                    while (entry != null)
                    {
                        yield return entry;
                        entry = GetNextEntry(reader, state);
                    }

                    if (state.Mode != Mode.Scanning)
                    {
                        throw state.CreateError(Parser.Errors.IncompleteToken);
                    }
                }

                yield break;
            }
        }

        /// <summary>
        /// Parses the document from the Stream, lazily.
        /// </summary>
        public static IEnumerable<Entry> Parse(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                State state = new State();

                var entry = GetNextEntry(reader, state);
                while (entry != null)
                {
                    yield return entry;
                    entry = GetNextEntry(reader, state);
                }

                if (state.Mode != Mode.Scanning)
                {
                    throw state.CreateError(Parser.Errors.IncompleteToken);
                }
            }

            yield break;
        }

        /// <summary>
        /// Reads the next Entry from the Stream.
        /// </summary>
        private static Entry GetNextEntry(StreamReader reader, State state)
        {
            Entry newEntry = null;
            while ((newEntry == null) || (newEntry.Parent != null))
            {
                int lineLength = (state.CurrentLine == null) ? 0 : state.CurrentLine.Length;
                if ((string.IsNullOrEmpty(state.CurrentLine)) || (state.Position >= lineLength))
                {
                    if (reader.EndOfStream)
                    {
                        newEntry = CloseCurrentToken(state);
                        return newEntry;
                    }

                    if (state.Mode == Mode.ReadingStringValue)
                    {
                        throw state.CreateError(Parser.Errors.UnexpectedNewLineInString);
                    }

                    newEntry = TryCloseCurrentToken(state);
                    if (newEntry != null)
                    {
                        if (newEntry.Parent == null)
                        {
                            return newEntry;
                        }
                    }

                    ++state.LineNumber;
                    state.CurrentLine = reader.ReadLine();
                    state.Position = 0;
                }

                if (state.Mode == Mode.ReadingStringValue)
                {
                    newEntry = ConsumeString(state);
                }
                else if (state.Mode == Mode.ReadingMultiLineStringValue)
                {
                    newEntry = ConsumeMultilineString(state);
                }
                else if (state.Mode == Mode.SearchingForValueSeparator)
                {
                    if (!state.NeedsNewLine)
                    {
                        ConsumeValueSeparator(state);
                    }
                }
                else if (state.Mode == Mode.ReadingValue)
                {
                    if (!state.NeedsNewLine)
                    {
                        newEntry = ConsumeValue(state);
                    }
                }
                else if (state.Mode == Mode.ReadingArrayEnd)
                {
                    newEntry = ConsumeArrayEnd(state);
                }
                else if (state.InArray)
                {
                    if (state.Mode == Mode.SearchingForArraySeparator)
                    {
                        ConsumeWhitespaceAndComments(state);
                        ConsumeArraySeparator(state);
                    }
                    else if (state.Mode == Mode.ReadingArrayEnd)
                    {
                        newEntry = ConsumeArrayEnd(state);
                    }
                    else
                    {
                        ConsumeValue(state);
                        if (state.Mode != Mode.ReadingMultiLineStringValue)
                        {
                            state.Mode = Mode.SearchingForArraySeparator;
                            ConsumeArraySeparator(state);
                        }
                    }
                }
                else
                {
                    newEntry = Consume(state);
                }

            } // end - while (newEntry == null)

            return newEntry;
        }

        /// <summary>
        /// Attempts to close the current token. Throws a ParserException
        /// if more data is required.
        /// </summary>
        /// <param name="state">The current parser state.</param>
        /// <returns>The closed token Entry.</returns>
        private static Entry CloseCurrentToken(State state)
        {
            // TODO: IMPLEMENT
            return null;
        }

        /// <summary>
        /// Attempts to close the current token. Does not throw if the Token is not closeable yet.
        /// </summary>
        /// <param name="state">The current parser state.</param>
        /// <returns>The closed token Entry.</returns>
        private static Entry TryCloseCurrentToken(State state)
        {
            // TODO: IMPLEMENT
            return null;
        }

        /// <summary>
        /// Finds and consumes the start of the next token.
        /// </summary>
        private static Entry Consume(State state)
        {
            // closes out the current token, if there is one.
            var entry = CloseCurrentToken(state);
            if (entry != null)
            {
                return entry;
            }

            // the 'none' state. We're looking for:
            //  1) Comment
            //  2) [
            //  3) any identifier start
            ConsumeWhitespaceAndComments(state);

            for (int idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (curChar == Parser.Tokens.KeyStart)
                {
                    state.Position = idx + 1;
                    ConsumeKeyName(state);
                    break;
                }
                else if (curChar == Parser.Tokens.Comment)
                {
                    state.Position = idx + 1;
                    ConsumeComment(state);
                    break;
                }
                else
                {
                    if (IsValidIdentifierStartChar(curChar))
                    {
                        state.Position = idx;
                        ConsumeValueName(state);
                        return null;
                    }
                    else
                    {
                        throw state.CreateError(idx, Parser.Errors.InvalidIdentifierCharacter);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Skips the whitespace in the current line, from the current position.
        /// </summary>
        private static void ConsumeWhitespace(State state)
        {
            int idx = state.Position;
            for (idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                if (!Parser.IsWhitespace(state.CurrentLine[idx]))
                {
                    state.Position = idx;
                    return;
                }
            }

            state.Position = idx;
            return;
        }

        /// <summary>
        /// Consumes a comment to the end of the line.
        /// </summary>
        private static void ConsumeComment(State state)
        {
            state.Position = state.CurrentLine.Length;
            return;
        }

        /// <summary>
        /// Skips the whitespace in the current line, from the current position.
        /// </summary>
        private static void ConsumeWhitespaceAndComments(State state)
        {
            int idx = state.Position;
            for (idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                if (!Parser.IsWhitespace(state.CurrentLine[idx]))
                {
                    state.Position = idx;
                    break;
                }
            }

            for ( ; idx < state.CurrentLine.Length; ++idx)
            {
                if (state.CurrentLine[idx] == Parser.Tokens.Comment)
                {
                    state.Position = state.CurrentLine.Length;
                    return;
                }

                break;
            }

            state.Position = idx;
            return;
        }

        /// <summary>
        /// Attempts to consume the name of the key.
        /// </summary>
        private static void ConsumeKeyName(State state)
        {
            int endBracketIdx = state.CurrentLine.IndexOf(Parser.Tokens.KeyEnd, state.Position);
            if (endBracketIdx == -1)
            {
                throw state.CreateError(state.CurrentLine.Length, Parser.Errors.UnexpectedNewLineInKeyName);
            }

            if (endBracketIdx - state.Position <= 1)
            {
                throw state.CreateError(Parser.Errors.KeyNameIsEmpty);
            }

            string keyName = state.CurrentLine.Substring(state.Position, endBracketIdx - state.Position).Trim();
            state.CurrentGroupName = keyName;

            state.Position = endBracketIdx + 1;
            state.Mode = Mode.Scanning;
            return;
        }

        /// <summary>
        /// Attempts to consume the value name, up to the equals.
        /// </summary>
        private static void ConsumeValueName(State state)
        {
            int nameEnd = -1;

            ConsumeWhitespace(state);
            int nameStart = state.Position;
            bool skipNext = false;

            for (int idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (IsWhitespace(curChar))
                {
                    nameEnd = idx;
                    break;
                }

                if (curChar == Parser.Tokens.ValueSeparator)
                {
                    nameEnd = idx;
                    skipNext = true;
                    break;
                }

                if (!IsValidIdentifierChar(curChar))
                {
                    throw state.CreateError(idx, Parser.Errors.ExpectingIdentifier);
                }
            }

            if (nameEnd == -1)
            {
                state.Position = state.CurrentLine.Length;
                return;
            }

            if (nameEnd - state.Position == 0)
            {
                throw state.CreateError(Parser.Errors.IdentifierNameIsEmpty);
            }

            state.CurrentValueName = state.CurrentLine.Substring(state.Position, nameEnd - state.Position);
            state.Position = nameEnd + (skipNext ? 1 : 0);

            // see if we can consume the Equals sign to
            state.Mode = (skipNext) ? Mode.ReadingValue : Mode.SearchingForValueSeparator;
            ConsumeWhitespace(state);

            if (state.Mode == Mode.SearchingForValueSeparator)
            {
                ConsumeValueSeparator(state);
            }

            return;
        }

        /// <summary>
        /// Looks for the Equals sign after a value identifier
        /// </summary>
        /// <param name="state">The current parser state.</param>
        private static void ConsumeValueSeparator(State state)
        {
            ConsumeWhitespace(state);

            int idx = state.CurrentLine.Length;
            for (idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (curChar == Parser.Tokens.Comment)
                {
                    ConsumeComment(state);
                    return;
                }

                if (curChar != Parser.Tokens.ValueSeparator)
                {
                    throw state.CreateError(idx, Parser.Errors.ExpectingAssignmentOperator);
                }

                state.Position = idx + 1;
                state.Mode = Mode.ReadingValue;
                return;
            }

            state.Position = idx;
            return;
        }

        /// <summary>
        /// Attempts to consume the non-string, non-array value.
        /// </summary>
        private static Entry ConsumeValue(State state)
        {
            ConsumeWhitespaceAndComments(state);

            string tokenBuilder = string.Empty;
            int idx = state.CurrentLine.Length;

            for (idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];

                if (curChar == Parser.Tokens.ArrayStart)
                {
                    state.CreateEntry(Entry.TomlType.Array);
                    state.Position = idx + 1;
                    state.Mode = Mode.ReadingValue;
                    return null;
                }

                if (curChar == Parser.Tokens.MultiLineQuoteStart)
                {
                    tokenBuilder += curChar;
                    if (tokenBuilder == Parser.Tokens.MultiLineQuoteStartToken)
                    {
                        state.Position = idx + 1;
                        state.Mode = Mode.ReadingMultiLineStringValue;
                        return null;
                    }

                    continue;
                }

                if (tokenBuilder == Parser.Tokens.EmptyStringToken)
                {
                    state.CurrentValue = string.Empty;
                    state.Position = idx;
                    state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
                    return state.CreateEntry(Entry.TomlType.String);
                }
                else if (tokenBuilder != string.Empty)
                {
                    state.Position = idx;
                    state.Mode = Mode.ReadingStringValue;
                    return null;
                }

                tokenBuilder = string.Empty;
                if (IsWhitespace(curChar))
                {
                    continue;
                }

                if (curChar == Parser.Tokens.Comment)
                {
                    state.Position = idx + 1;
                    state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
                    ConsumeComment(state);
                    return null;
                }

                if ((char.IsDigit(curChar)) || (curChar == '-'))
                {
                    state.Position = idx;
                    return ConsumeNumber(state);
                }

                if ((curChar == 't') || (curChar == 'T') ||
                    (curChar == 'f') || (curChar == 'F'))
                {
                    state.Position = idx;
                    return ConsumeBoolean(state, ((curChar == 't') || (curChar == 'T')));
                }

                if ((curChar == Parser.Tokens.ArrayEnd) && (state.InArray))
                {
                    var array = state.CloseArray();
                    state.Position = idx + 1;
                    state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;

                    return array;
                }

                throw state.CreateError(idx, Parser.Errors.InvalidValue);
            }

            state.Position = idx;
            return null;
        }

        /// <summary>
        /// Consumes the characters in the string, until the end of the string, or the end of the line.
        /// </summary>
        private static Entry ConsumeString(State state)
        {
            bool isEscaping = false;
            string value = string.Empty;
            int lastConsumedPos = state.Position;

            for (int idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (isEscaping)
                {
                    isEscaping = false;
                    value += GetEscapedCharacter(idx, curChar, state);
                    lastConsumedPos = idx + 1;
                    continue;
                }

                if (curChar == Parser.Tokens.EscapeChar)
                {
                    isEscaping = true;
                    value += state.CurrentLine.Substring(lastConsumedPos, idx - lastConsumedPos);
                    continue;
                }

                if (curChar == Parser.Tokens.QuoteEnd)
                {
                    value += state.CurrentLine.Substring(lastConsumedPos, idx - lastConsumedPos);

                    state.CurrentValue = value;
                    state.Position = idx + 1;
                    state.Mode = (state.ArrayDepth > 0) ? Mode.SearchingForArraySeparator : Mode.Scanning;

                    return state.CreateEntry(Entry.TomlType.String);
                }
            }

            state.CurrentValue = value;
            throw state.CreateError(Parser.Errors.UnexpectedNewLineInString);
        }

        /// <summary>
        /// Consumes the characters in the multi-line string, until the end of the string.
        /// </summary>
        private static Entry ConsumeMultilineString(State state)
        {
            bool isEscaping = false;
            int lastConsumedPos = state.Position;
            string tokenBuilder = string.Empty;

            for (int idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (isEscaping)
                {
                    isEscaping = false;
                    if ((curChar == Parser.Tokens.MultiLineQuoteStart) && (idx + 2 < state.CurrentLine.Length))
                    {
                        state.CurrentValue += state.CurrentLine.Substring(lastConsumedPos, idx - lastConsumedPos);
                        state.CurrentValue += Parser.Tokens.MultiLineQuoteStart;
                        idx += 2;
                        continue;
                    }
                    else
                    {
                        state.CurrentValue += GetEscapedCharacter(idx, curChar, state);
                    }

                    lastConsumedPos = idx + 1;
                    continue;
                }

                if (curChar == Parser.Tokens.EscapeChar)
                {
                    tokenBuilder = string.Empty;
                    isEscaping = true;
                    state.CurrentValue += state.CurrentLine.Substring(lastConsumedPos, idx - lastConsumedPos);
                    continue;
                }

                if (curChar == Parser.Tokens.MultiLineQuoteEnd)
                {
                    tokenBuilder += curChar;
                    if (tokenBuilder == Parser.Tokens.MultiLineQuoteEndToken)
                    {
                        // we're done
                        state.CurrentValue += state.CurrentLine.Substring(lastConsumedPos, idx - lastConsumedPos - 2);
                        lastConsumedPos = idx + 1;
                        state.Position = lastConsumedPos;
                        state.Mode = (state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning);
                        return state.CreateEntry(Entry.TomlType.String);
                    }
                }
                else
                {
                    tokenBuilder = string.Empty;
                }
            }

            state.CurrentValue += state.CurrentLine.Substring(lastConsumedPos, state.CurrentLine.Length - lastConsumedPos);
            state.CurrentValue += Environment.NewLine;
            state.Position = state.CurrentLine.Length;
            return null;
        }

        /// <summary>
        /// Attempts to read a boolean from the state's CurrentLine.
        /// </summary>
        private static Entry ConsumeBoolean(State state, bool expectedValue)
        {
            if (expectedValue)
            {
                if (state.CurrentLine.Length - state.Position < 4)
                {
                    throw state.CreateError(Parser.Errors.InvalidValue);
                }

                if (!state.CurrentLine.Substring(state.Position, 4).Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    throw state.CreateError(Parser.Errors.InvalidValue);
                }

                state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
                state.CurrentValue = true.ToString();
                return state.CreateEntry(Entry.TomlType.Boolean);
            }
            else if (state.CurrentLine.Length - state.Position < 5)
            {
                throw state.CreateError(Parser.Errors.InvalidValue);
            }

            if (!state.CurrentLine.Substring(state.Position, 5).Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                throw state.CreateError(Parser.Errors.InvalidValue);
            }

            state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
            state.CurrentValue = false.ToString();
            return state.CreateEntry(Entry.TomlType.Boolean);
        }

        /// <summary>
        /// Attempts to read a number from the state's CurrentLine.
        /// </summary>
        private static Entry ConsumeNumber(State state)
        {
            bool receivedDecimal = false;
            int dashCount = 0;
            int digitCount = 0;

            int idx;
            for (idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (IsWhitespace(curChar))
                {
                    break;
                }

                if (curChar == Parser.Tokens.Comment)
                {
                    throw state.CreateError(idx, Parser.Errors.UnexpectedCommentInValue);
                }

                if (curChar == Parser.Tokens.Negative)
                {
                    if (dashCount == 0)
                    {
                        ++dashCount;
                        if ((digitCount == 4) || (digitCount == 0))
                        {
                            continue;
                        }

                        throw state.CreateError(idx, Parser.Errors.InvalidValue);
                    }
                    else if (dashCount == 1)
                    {
                        ++dashCount;
                        if ((digitCount == 5) || (digitCount == 6))
                        {
                            continue;
                        }

                        throw state.CreateError(idx, Parser.Errors.InvalidValue);
                    }
                    else if (dashCount == 2)
                    {
                        ++dashCount;
                        if ((digitCount == 6) || (digitCount == 7) || (digitCount == 8))
                        {
                            return ConsumeDateTime(state);
                        }
                    }
                    else
                    {
                        throw state.CreateError(idx, Parser.Errors.InvalidValue);
                    }
                }

                if (curChar == Parser.Tokens.Decimal)
                {
                    if (receivedDecimal)
                    {
                        throw state.CreateError(idx, Parser.Errors.InvalidValue);
                    }

                    receivedDecimal = true;
                }

                if (!char.IsDigit(curChar))
                {
                    if ((dashCount == 2) && ((curChar == 'T') || (IsWhitespace(curChar))))
                    {
                        return ConsumeDateTime(state);
                    }
                    if ((curChar != Parser.Tokens.KeyStart) && (!IsWhitespace(curChar)))
                    {
                        if (state.InArray && curChar != Parser.Tokens.ArraySeparator && curChar != Parser.Tokens.ArrayEnd)
                        {
                            throw state.CreateError(idx, Parser.Errors.InvalidValue);
                        }

                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    ++digitCount;
                }
            }

            state.CurrentValue = state.CurrentLine.Substring(state.Position, idx - state.Position);
            if (receivedDecimal)
            {
                state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
                state.Position = idx;
                return state.CreateEntry(Entry.TomlType.Float);
            }

            if (digitCount > 0)
            {
                state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
                state.Position = idx;
                return state.CreateEntry(Entry.TomlType.Int);
            }

            throw state.CreateError(idx, Parser.Errors.InvalidValue);
        }

        /// <summary>
        /// Attempts to read a DateTime from the state's CurrentLine.
        /// </summary>
        private static Entry ConsumeDateTime(State state)
        {
            // search for a new line or a [ or a #
            int idx;
            for (idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char curChar = state.CurrentLine[idx];
                if (curChar == Parser.Tokens.KeyStart)
                {
                    --idx;
                    break;
                }
                else if (curChar == Parser.Tokens.Comment)
                {
                    --idx;
                    break;
                }
                else if ((curChar != ' ') && (IsWhitespace(curChar)))
                {
                    --idx;
                    break;
                }
            }

            state.CurrentValue = state.CurrentLine.Substring(state.Position, idx - state.Position);
            state.Position = idx;

            DateTime result;
            if (!DateTime.TryParse(state.CurrentValue, out result))
            {
                throw state.CreateError(idx, Parser.Errors.InvalidValue);
            }

            state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
            return state.CreateEntry(Entry.TomlType.DateTime);
        }

        /// <summary>
        /// Looks for the comma after an array value
        /// </summary>
        /// <param name="state">The current Parser state.</param>
        private static void ConsumeArraySeparator(State state)
        {
            ConsumeWhitespaceAndComments(state);

            int idx = state.CurrentLine.Length;
            for (idx = state.Position; idx < state.CurrentLine.Length; )
            {
                char curChar = state.CurrentLine[idx];
                if (curChar == Parser.Tokens.ArrayEnd)
                {
                    state.Position = idx;
                    state.Mode = Mode.ReadingArrayEnd;
                    return;
                }

                if (curChar != Parser.Tokens.ArraySeparator)
                {
                    throw state.CreateError(idx, Parser.Errors.ExpectingArraySeparator);
                }

                state.Position = idx + 1;
                state.Mode = Mode.ReadingValue;
                return;
            }

            state.Position = idx;
            return;
        }

        /// <summary>
        /// Attempts to consume the array to the end, or the end of the line.
        /// </summary>
        private static Entry ConsumeArrayEnd(State state)
        {
            ConsumeWhitespace(state);
            for (int idx = state.Position; idx < state.CurrentLine.Length; ++idx)
            {
                char lineChar = state.CurrentLine[idx];

                if (lineChar == Parser.Tokens.ArrayEnd)
                {
                    Array rootArray = state.CloseArray();

                    state.Position = idx + 1;
                    state.Mode = state.InArray ? Mode.SearchingForArraySeparator : Mode.Scanning;
                    return rootArray;
                }
            }

            return null;
        }
    }
}
