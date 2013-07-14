using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    /// <summary>
    /// Represents a identifier = value entry in the document.
    /// </summary>
    public class Entry
    {
        /// <summary>
        /// Supported TOML types.
        /// </summary>
        public enum TomlType
        {
            Unknown,
            String,
            Int,
            Float,
            DateTime,
            Boolean,
            Array
        }

        /// <summary>
        /// Initializes a new instance of the Entry class.
        /// </summary>
        public Entry(string group, string name, string source, int startLineNumber, int startPos, TomlType parsedType)
        {
            this.Group = group ?? string.Empty;
            this.Name = name;
            this.SourceText = source;
            this.LineNumber = startLineNumber;
            this.Position = startPos;
            this.ParsedType = parsedType;
        }

        /// <summary>
        /// Initializes a new instance of the Entry class.
        /// </summary>
        public Entry(Array parent, string group, string source, int startLineNumber, int startPos, TomlType parsedType)
        {
            this.Parent = parent;
            this.Group = group ?? string.Empty;
            this.Name = Array.GetEntryName(parent);
            this.SourceText = source;
            this.LineNumber = startLineNumber;
            this.Position = startPos;
            this.ParsedType = parsedType;
        }

        /// <summary>
        /// Returns a string representing the value
        /// </summary>
        /// <returns>A string representing the value</returns>
        public override string ToString()
        {
            if (this.Parent == null)
            {
                return string.Format
                (
                    "{0} {1} = {3}{2}{3}",
                    this.ParsedType.ToString(),
                    this.FullName,
                    this.SourceText,
                    this.ParsedType == TomlType.String ? "\"" : string.Empty
                );
            }

            if (this.ParsedType == TomlType.String)
            {
                return "\"" + this.SourceText + "\"";
            }

            return this.SourceText;
        }

        /// <summary>
        /// The name of the group the value belongs to.
        /// </summary>
        public string Group { get; private set; }

        /// <summary>
        /// Gets the full name of the entry.
        /// </summary>
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(this.Group))
                {
                    return this.Name;
                }

                return this.Group + "." + this.Name;
            }
        }

        /// <summary>
        /// The name of the value item.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The text of the value.
        /// </summary>
        public string SourceText { get; protected set; }

        /// <summary>
        /// Gets the 1-based line number the value starts at.
        /// </summary>
        public int LineNumber { get; private set; }

        /// <summary>
        /// Gets the 0-based position on the line the value starts at.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// The array (if any) that owns this entry.
        /// </summary>
        public Array Parent { get; private set; }

        /// <summary>
        /// Gets the type that the lexer parsed the entry as.
        /// </summary>
        public TomlType ParsedType { get; private set; }
    }
}
