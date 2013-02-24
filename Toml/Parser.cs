using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    public static class Parser
    {
        public class ReservedTokens
        {
            public static readonly string Comment    = "#";
            public static readonly string Equals     = "=";

            public static readonly string StartKey   = "[";
            public static readonly string EndKey     = "]";

            public static readonly string Separator  = ".";
            
            public static readonly string StartArray = "[";
            public static readonly string EndArray   = "]";

            public static readonly string Quote = "\"";
            public static readonly string AppendToString = "+";
            public static readonly string Escape = "\\";

            public static readonly string[] All = 
            {
                Comment,
                Equals,
                StartKey,
                EndKey,
                Separator,
                StartArray,
                EndArray
            };

            public static readonly string[] AllExceptSeparator = 
            {
                Comment,
                Equals,
                StartKey,
                EndKey,
                StartArray,
                EndArray
            };

            /// <summary>
            /// Indicates whether or not the specified token is reserved.
            /// </summary>
            /// <param name="token"></param>
            /// <returns></returns>
            public static bool IsReservedToken(string token)
            {
                return ReservedTokens.All.Any(t => t.Equals(token, StringComparison.OrdinalIgnoreCase));
            }
        }

        public class EscapedChars
        {
            public static string GetEscapedCharValue(char escapedChar)
            {
                switch (escapedChar)
                {
                    case (Toml.Parser.EscapedChars.Newline):
                        return "\n";

                    case (Toml.Parser.EscapedChars.Return):
                        return "\r";
                    
                    case (Toml.Parser.EscapedChars.Null):
                        return "\0";
                    
                    case (Toml.Parser.EscapedChars.Tab):
                        return "\t";
                    
                    case (Toml.Parser.EscapedChars.Backslash):
                        return "\\";

                    case (Toml.Parser.EscapedChars.Quote):
                        return "\"";
                }

                throw new InvalidOperationException("Invalid Escape Character");
            }

            public const char Newline = 'n';
            public const char Return = 'r';
            public const char Tab = 't';
            public const char Null = '0';
            public const char Backslash = '\\';
            public const char Quote = '\"';
        }

        private class State
        {
            public enum Mode
            {
                None,
                ReceivingGroupKey,
                ReceivingValueKey,
                ReceivingValue
            }

            /// <summary>
            /// Initializes a new instance of the Parser.State class.
            /// </summary>
            public State()
            {
                this.Document = Document.Create();
                this.CurrentMode = Mode.None;
                this.CurrentGroup = this.Document;
            }

            /// <summary>
            /// Gets the Mode the parser is currently in.
            /// </summary>
            public Mode CurrentMode { get; set; }

            /// <summary>
            /// The Document being generated.
            /// </summary>
            public Document Document { get; private set; }

            /// <summary>
            /// The current group items are being added to.
            /// </summary>
            public Group CurrentGroup { get; set; }

            /// <summary>
            /// Gets the name of the current key being generated.
            /// </summary>
            public string CurrentValueKey { get; set; }

            /// <summary>
            /// Gets the current value of the item being read.
            /// </summary>
            public string CurrentValue { get; set; }

            /// <summary>
            /// The last token encountered by the parser.
            /// </summary>
            public string LastToken { get; set; }

            /// <summary>
            /// Indicates whether or not the Parser just encountered an escape char.
            /// </summary>
            public bool IsEscaping { get; set; }

            /// <summary>
            /// Indicates whether or not the parser is currently parsing a quoted string.
            /// </summary>
            public bool IsInQuotes { get; set; }

            /// <summary>
            /// Gets the number of levels deep in an array the parser currently is.
            /// </summary>
            public int ArrayDepth { get; set; }

            /// <summary>
            /// Indicates whether or not the parser is currently parsing an array.
            /// </summary>
            public bool IsInArray
            {
                get
                {
                    return this.ArrayDepth > 0;
                }
            }

            /// <summary>
            /// Adds the specified value to the current group.
            /// </summary>
            public void AddValue(string name, string value)
            {
                if (this.CurrentGroup == null)
                {
                    throw new InvalidOperationException("A Group must be defined before values can be added");
                }

                this.CurrentGroup.AddValue(name, value);
            }

            /// <summary>
            /// Adds the specified Group to the Document.
            /// </summary>
            public void AddGroup(string key)
            {
                this.CurrentGroup = this.Document.CreateGroup(key);
            }

            /// <summary>
            /// Validates the parser is prepared to receive a new line. If the parser
            /// is not prepared for a new line, an exception will be thrown.
            /// If the parser is currently parsing a value, the current value will be saved.
            /// </summary>
            public void CompleteLine()
            {
                if (this.IsEscaping || this.IsInQuotes)
                {
                    throw new InvalidOperationException("The parser is not ready to receive a new line");
                }
            }

            public void EnterArray()
            {
                ++this.ArrayDepth;
            }

            public bool LeaveArray()
            {
                if (this.ArrayDepth <= 0)
                {
                    throw new InvalidOperationException("ArrayDepth");
                }

                this.ArrayDepth -= 1;
                return this.IsInArray;
            }
        }

        /// <summary>
        /// Attempts to parse the specified Stream into a Toml.Document.
        /// </summary>
        /// <param name="stream">The stream to parse.</param>
        /// <returns>A new Toml.Document generated from the specified stream.</returns>
        public static Document Parse(Stream stream)
        {
            Parser.State state = new State();
            int lineNumber = 0;
            string line = string.Empty;

            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    ++lineNumber;

                    line = reader.ReadLine();
                    ParseLine(state, line);
                }
            }

            if (!string.IsNullOrEmpty(state.CurrentValueKey))
            {
                state.CurrentGroup.AddValue(state.CurrentValueKey, state.CurrentValue);
            }

            return state.Document;
        }

        /// <summary>
        /// Parsers the specified line into the Document.
        /// </summary>
        /// <param name="state">The object used to store the parser's state.</param>
        /// <param name="line">The line being parsed.</param>
        private static void ParseLine(Parser.State state, string line)
        {
            // remove the whitespace
            line = line.Trim();

            if ((string.IsNullOrEmpty(line)) || (line.StartsWith(Toml.Parser.ReservedTokens.Comment)))
            {
                return;
            }

            if ((state.CurrentMode == State.Mode.ReceivingGroupKey) || (state.CurrentMode == State.Mode.ReceivingValueKey))
            {
                throw new InvalidOperationException("Name cannot span multiple lines");
            }

            if (state.IsInQuotes)
            {
                throw new InvalidOperationException("Newline in constant string expression");
            }

            if (state.CurrentMode == State.Mode.None)
            {
                if (state.LastToken == Toml.Parser.ReservedTokens.Quote)
                {
                    if (line.StartsWith(Toml.Parser.ReservedTokens.AppendToString))
                    {
                        // building on to the previous line's string value
                        state.LastToken = Toml.Parser.ReservedTokens.AppendToString;
                        line = line.Substring(1);

                        ParseLine(state, line);
                        return;
                    }

                    if ((state.CurrentValue != null) && (state.CurrentValueKey != null))
                    {
                        state.CurrentGroup.AddValue(state.CurrentValueKey, state.CurrentValue);
                        state.CurrentValue = null;
                        state.CurrentValueKey = null;
                    }

                    state.LastToken = null;
                }
                else if (state.LastToken == Toml.Parser.ReservedTokens.AppendToString)
                {
                    if (!line.StartsWith(Toml.Parser.ReservedTokens.Quote))
                    {
                        throw new InvalidOperationException("Expected continuation of previous line's string value");
                    }

                    state.CurrentMode = State.Mode.ReceivingValue;
                    state.LastToken = Toml.Parser.ReservedTokens.Quote;
                }

                if (state.CurrentMode != State.Mode.ReceivingValue)
                {
                    // we're looking for a key start, or a value name
                    if (line.StartsWith(Toml.Parser.ReservedTokens.StartKey))
                    {
                        state.CurrentMode = State.Mode.ReceivingGroupKey;

                        int endKeyName = line.IndexOf(Toml.Parser.ReservedTokens.EndKey);
                        if (endKeyName <= 1)
                        {
                            throw new InvalidOperationException("KeyName cannot be Empty, and cannot span multiple lines");
                        }

                        string keyName = line.Substring(1, endKeyName - 1);
                        state.AddGroup(keyName);
                        if (line.Length <= endKeyName + 1)
                        {
                            state.CurrentMode = State.Mode.None;
                            state.CompleteLine();
                            return;
                        }

                        string lineRemaining = line.Substring(endKeyName + 1);
                        lineRemaining = lineRemaining.Trim();

                        state.CurrentMode = State.Mode.None;
                        ParseLine(state, lineRemaining);
                        return;
                    }
                }

                // read up to the equal sign
                int valueNameEnd = line.IndexOf(Toml.Parser.ReservedTokens.Equals);
                if (valueNameEnd > 0)
                {
                    state.CurrentMode = State.Mode.ReceivingValueKey;
                    string valueName = line.Substring(0, valueNameEnd).Trim();
                    if (string.IsNullOrWhiteSpace(valueName))
                    {
                        throw new InvalidOperationException("Empty ValueName");
                    }

                    state.CurrentValueKey = valueName.Trim();
                    state.CurrentMode = State.Mode.ReceivingValue;

                    if (line.Length <= valueNameEnd + 1)
                    {
                        state.CurrentMode = State.Mode.None;
                        state.CompleteLine();
                        return;
                    }

                    string lineRemaining = line.Substring(valueNameEnd + 1);
                    lineRemaining = lineRemaining.Trim();

                    state.CurrentMode = State.Mode.ReceivingValue;
                    ParseLine(state, lineRemaining);
                    return;
                }
            }

            int linePos = 0;
            int quoteStart = -1;
            int arrayStart = -1;

            foreach (var lineChar in line)
            {
                if (lineChar == Toml.Parser.ReservedTokens.Quote[0])
                {
                    if (state.IsInQuotes)
                    {
                        string quotedValue = null;
                        if (!state.IsEscaping)
                        {
                            state.IsInQuotes = false;

                            if (state.IsInArray)
                            {
                                quotedValue = line.Substring(quoteStart - 1, linePos - quoteStart + 2);
                                state.CurrentValue += quotedValue;
                            }
                            else
                            {
                                quotedValue = line.Substring(quoteStart, linePos - quoteStart);
                                state.CurrentValue += quotedValue;
                            }

                            state.LastToken = Toml.Parser.ReservedTokens.Quote;
                            if (!state.IsInArray)
                            {
                                state.CurrentMode = State.Mode.None;
                            }

                            line = line.Substring(linePos + 1).Trim();
                            ParseLine(state, line);
                            return;
                        }

                        quotedValue = line.Substring(quoteStart, linePos - 2);
                        state.CurrentValue += quotedValue;
                        quoteStart = linePos;

                        ++linePos;
                        continue;
                    }
                    else
                    {
                        state.IsInQuotes = true;
                        quoteStart = linePos + 1;

                        ++linePos;
                        continue;
                    }
                }
                else if (lineChar == Toml.Parser.ReservedTokens.Escape[0])
                {
                    if (!state.IsInQuotes)
                    {
                        throw new InvalidOperationException("Escape Char outside of String");
                    }

                    state.IsEscaping = true;
                    state.CurrentValue += line.Substring(quoteStart, linePos - 1);
                    ++linePos;
                    continue;
                }
                else if (state.IsEscaping)
                {
                    // process escape chars
                    state.CurrentValue += Toml.Parser.EscapedChars.GetEscapedCharValue(lineChar);
                    state.IsEscaping = false;
                    
                    quoteStart = linePos + 1;
                    ++linePos;
                    continue;
                }

                if (state.IsInQuotes)
                {
                    ++linePos;
                    continue;
                }

                if (lineChar == Toml.Parser.ReservedTokens.Equals[0])
                {
                    // we're already receiving a value - this is illegal
                    throw new InvalidOperationException("Unexpected token found");
                }

                if (state.IsInArray)
                {
                    state.CurrentValue += lineChar;
                    if (lineChar == Toml.Parser.ReservedTokens.StartArray[0])
                    {
                        state.EnterArray();
                        ++linePos;
                        continue;
                    }
                    else if (lineChar == Toml.Parser.ReservedTokens.EndArray[0])
                    {
                        state.LeaveArray();
                        if (!state.IsInArray)
                        {
                            state.CurrentGroup.AddValue(state.CurrentValueKey, state.CurrentValue);
                            state.CurrentValue = null;
                            state.CurrentValueKey = null;

                            state.CurrentMode = State.Mode.None;
                            line = line.Substring(linePos + 1);
                            ParseLine(state, line);
                            return;
                        }

                        ++linePos;
                        continue;
                    }

                    ++linePos;
                    continue;
                }
                else if (lineChar == Toml.Parser.ReservedTokens.StartArray[0])
                {
                    arrayStart = linePos;
                    state.CurrentValue = lineChar.ToString();

                    state.EnterArray();
                    ++linePos;
                    continue;
                }

                if (lineChar == Toml.Parser.ReservedTokens.Comment[0])
                {
                    return;
                }

                // we have a value that's not in an array, and not in quotes
                // just read to the end of the line, or to a comment, and that's the current value
                string currentValue = line.Trim();
                int commentStart = currentValue.IndexOf(Toml.Parser.ReservedTokens.Comment);
                if (commentStart != -1)
                {
                    state.CurrentValue = currentValue.Substring(0, commentStart - 1);
                }
                else
                {
                    state.CurrentValue = currentValue;
                }

                state.AddValue(state.CurrentValueKey, state.CurrentValue);
                state.CurrentValueKey = null;
                state.CurrentValue = null;

                state.CurrentMode = State.Mode.None;
                return;
            }

            return;
        }
    }
}
