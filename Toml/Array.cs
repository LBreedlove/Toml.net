using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{

    /// <summary>
    /// Represents an array entry
    /// </summary>
    public class Array : Entry
    {
        public class Dimensions
        {
            public int Depth { get; set; }
            public int Length { get; set; }
        }

        /// <summary>
        /// Array containing one element with a value of 0, used for Min/Max calls
        /// that require a non-empty sequence.
        /// </summary>
        private static readonly int[] EnumerableOfZero = new int[] { 0 };

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Entry class.
        /// </summary>
        public Array(string group, string name, int startLineNumber, int startPos)
            : base(group, name, string.Empty, startLineNumber, startPos, TomlType.Array)
        {
            this.Children = new List<Entry>();
        }

        /// <summary>
        /// Initializes a new instance of the Entry class.
        /// </summary>
        public Array(Array parent, int startLineNumber, int startPos)
            : base(parent, parent.Group, Array.GetEntryName(parent), startLineNumber, startPos, TomlType.Array)
        {
            this.Children = new List<Entry>();
            parent.AddEntry(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the entries owned by this array.
        /// </summary>
        public List<Entry> Children { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a name that can be used to identify an array
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static string GetEntryName(Array parent)
        {
            if (parent == null)
            {
                System.Diagnostics.Debug.Assert(parent != null);
                return string.Empty;
            }

            int pos = parent.Children.Count;
            return pos.ToString();
        }

        /// <summary>
        /// Adds an entry to the array.
        /// </summary>
        /// <param name="entry">The entry to add to the array.</param>
        public void AddEntry(Entry entry)
        {
            this.Children.Add(entry);
        }

        /// <summary>
        /// Instructs the array to calculate its SourceText property.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <remarks>
        /// This is handled outside of the constructor for arrays, because we don't
        /// know where the sourceText will end until the array gets closed.
        /// </remarks>
        public void UpdateSourceText()
        {
            this.SourceText = string.Format
            (
                "{0}{1}{2}",
                Parser.Tokens.ArrayStart,
                String.Join(Parser.Tokens.ArraySeparator.ToString(), this.Children.Select(c => c.SourceText)),
                Parser.Tokens.ArrayEnd
            );
        }

        /// <summary>
        /// Gets a string representing the array.
        /// </summary>
        /// <returns>A string representing the array entry.</returns>
        public override string ToString()
        {
            StringBuilder valueText = new StringBuilder();
            if (this.Parent == null)
            {
                valueText.AppendFormat("Array {0} = ", this.FullName);
            }

            valueText.Append(Parser.Tokens.ArrayStart);
            valueText.Append(string.Join(Parser.Tokens.ArraySeparator + " ", this.Children.Select(c => c.ToString())));
            valueText.Append(Parser.Tokens.ArrayEnd);

            return valueText.ToString();
        }

        /// <summary>
        /// Gets a type that can be used to represent the array.
        /// </summary>
        /// <returns></returns>
        public Type GetArrayType()
        {
            var dimensions = GetDimensions();

            if (dimensions.Depth == 1)
            {
                if (dimensions.Length > 0)
                {
                    Type currentType = null;
                    foreach (var child in this.Children)
                    {
                        if (!GetBestType(ref currentType, Array.TomlTypeToNative(child.ParsedType)))
                        {
                            return typeof(object).MakeArrayType();
                        }
                    }

                    return (currentType == null) ? typeof(object).MakeArrayType() : currentType.MakeArrayType();
                }

                return typeof(object).MakeArrayType();
            }

            if (dimensions.Length > 0)
            {
                var childTypes = this.Children
                                    .Where(c => c.ParsedType == TomlType.Array)
                                    .Cast<Toml.Array>()
                                    .Select(ta => ta.GetArrayType());

                if (childTypes.Count() == 0)
                {
                    return typeof(object).MakeArrayType();
                }

                if (childTypes.All(at => at.Equals(childTypes.First())))
                {
                    return childTypes.First().MakeArrayType();
                }
            }

            return typeof(object).MakeArrayType();
        }

        /// <summary>
        /// Gets the native type that corresponds to the specified TomlType.
        /// </summary>
        /// <param name="type">The TomlType to find the corresponding Native Type for.</param>
        /// <returns>A Type that corresponds to the TomlType.</returns>
        private static Type TomlTypeToNative(TomlType type)
        {
            switch (type)
            {
                case TomlType.Int: return typeof(Int64);
                case TomlType.Float: return typeof(double);
                case TomlType.Boolean: return typeof(bool);
                case TomlType.DateTime: return typeof(DateTime);
                case TomlType.String: return typeof(string);
                default: return typeof(object);
            }
        }

        /// <summary>
        /// Used to find the best type for the elements in an array.
        /// </summary>
        /// <param name="currentType"></param>
        /// <param name="newType"></param>
        /// <returns></returns>
        private bool GetBestType(ref Type currentType, Type newType)
        {
            if (newType == typeof(Int64))
            {
                if ((currentType == null) || (currentType == typeof(Int64)))
                {
                    currentType = typeof(Int64);
                    return true;
                }

                if (currentType == typeof(double))
                {
                    currentType = typeof(double);
                    return true;
                }

                return false;
            }
            if (newType == typeof(double))
            {
                if ((currentType == null) || (currentType == typeof(double)))
                {
                    currentType = typeof(double);
                    return true;
                }

                if (currentType == typeof(double))
                {
                    currentType = typeof(double);
                    return true;
                }

                return false;
            }
            if (newType == typeof(bool))
            {
                if ((currentType == null) || (currentType == typeof(bool)))
                {
                    currentType = typeof(bool);
                    return true;
                }

                if ((currentType == typeof(Int64)) || (currentType == typeof(double)) || (currentType == typeof(string)))
                {
                    return true;
                }
                return false;
            }
            if (newType == typeof(DateTime))
            {
                if ((currentType == null) || (currentType == typeof(DateTime)))
                {
                    currentType = typeof(DateTime);
                    return true;
                }

                if (currentType == typeof(string))
                {
                    return true;
                }
                return false;
            }
            if (newType == typeof(string))
            {
                if ((currentType == typeof(double)) || (currentType == typeof(Int64)))
                {
                    return false;
                }

                currentType = typeof(string);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the max depth and length of the children/child arrays.
        /// </summary>
        /// <returns>The depth of the deepest child in the array.</returns>
        private Array.Dimensions GetDimensions()
        {
            return new Dimensions() { Depth = GetMaxDepth(), Length = GetMaxLength() };
        }

        /// <summary>
        /// Gets the deepest child in the array.
        /// </summary>
        /// <returns>The depth of the deepest child in the array.</returns>
        private int GetMaxDepth()
        {
            return 1 + this.Children
                           .Where(c => c.ParsedType == TomlType.Array)
                           .Cast<Toml.Array>()
                           .Select(c => c.GetMaxDepth())
                           .Concat(Array.EnumerableOfZero)
                           .Max();
        }

        /// <summary>
        /// Gets the maximum length of all the child arrays in this array, or the length of this array if it's the longest.
        /// </summary>
        /// <returns></returns>
        private int GetMaxLength()
        {
            int maxChildLength = this.Children
                                     .Where(c => c.ParsedType == TomlType.Array)
                                     .Cast<Toml.Array>()
                                     .Select(c => c.GetMaxDepth())
                                     .Concat(Array.EnumerableOfZero)
                                     .Max();

            return this.Children.Count > maxChildLength ? this.Children.Count : maxChildLength;
        }

        #endregion
    }
}
