using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    public class Serializer
    {
        /// <summary>
        /// Defines the types that can be serialized directly, without recursion.
        /// </summary>
        private static readonly Dictionary<Type, Func<object, string>> NativeTypes = new Dictionary<Type,Func<object,string>>()
        {
            { typeof(bool), (val) => val.ToString() },

            { typeof(SByte), (val) => val.ToString() },
            { typeof(Byte), (val) => val.ToString() },
            { typeof(Int16), (val) => val.ToString() },
            { typeof(UInt16), (val) => val.ToString() },
            { typeof(Int32), (val) => val.ToString() },
            { typeof(UInt32), (val) => val.ToString() },

            { typeof(float), (val) => val.ToString() },
            { typeof(double), (val) => val.ToString() },

            { typeof(string), (val) => EscapeAndQuoteString((string)val) },
            { typeof(DateTime), (val) => val.ToString() }
        };

        /// <summary>
        /// Serializes the specified object to the StreamWriter, using the specified rootKeyGroup as the groupName
        /// for the values.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="value">The value of the object to serialize.</param>
        /// <param name="rootKeyGroup">The key group to use to serialize the object.</param>
        /// <param name="writer">The StreamWriter to write with.</param>
        public static void Write<T>(T value, string rootKeyGroup, StreamWriter writer)
        {
            Serialize(typeof(T), value, rootKeyGroup, writer);
        }

        private static void Serialize(Type type, object value, string keyGroup, StreamWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(keyGroup))
            {
                keyGroup = keyGroup.TrimStart(Toml.Parser.Tokens.KeyStart)
                                   .TrimEnd(Toml.Parser.Tokens.KeyEnd)
                                   .Trim(Toml.Parser.Tokens.KeySeparator);

                writer.WriteLine("{0}{1}{2}", Toml.Parser.Tokens.KeyStart, keyGroup, Toml.Parser.Tokens.KeyEnd);
            }

            var properties = type.GetProperties()
                                 .Where(p => p.CanRead)
                                 .Where
                                 (
                                    p => !p.GetCustomAttributes(typeof(NonSerializedAttribute)).Any()
                                 );

            WriteNativeProperties(properties, value, writer);
            WriteArrayProperties(properties, value, writer);
            WriteComplexProperties(properties, value, keyGroup, writer);
            return;
        }

        /// <summary>
        /// Writes the native properties to the stream.
        /// </summary>
        private static bool WriteNativeProperties(IEnumerable<PropertyInfo> properties, object owner, StreamWriter writer)
        {
            var nativeProperties = properties.Where(p => Serializer.IsNativeType(p.PropertyType));
            return nativeProperties.Select
            (
                p => string.Format
                (
                    "{0} {1} {2}",
                    p.Name,
                    Toml.Parser.Tokens.ValueSeparator,
                    GetNativeValueString(p.PropertyType, p.GetValue(owner))
                )
            )
            .Select(ps => { writer.WriteLine(ps); return true; })
            .All(ps => ps);
        }

        /// <summary>
        /// Writes properties of complex types.
        /// </summary>
        private static bool WriteComplexProperties(IEnumerable<PropertyInfo> properties, object owner, string keyGroup, StreamWriter writer)
        {
            var complexProperties = properties.Where
            (
                p => !Serializer.IsNativeType(p.PropertyType) && !IsArrayType(p.PropertyType, p.GetValue(owner))
            ); 
            
            return complexProperties
                .Select
                (
                    p =>
                    {
                        var val = p.GetValue(owner);
                        if (val != null)
                        {
                            writer.WriteLine();
                            Serialize(p.PropertyType, val, keyGroup + Toml.Parser.Tokens.KeySeparator + p.Name, writer);
                            return true;
                        }

                        return true;
                    }
                ).All(p => p);
        }

        /// <summary>
        /// Write the values of the arrays to the stream.
        /// </summary>
        /// <param name="arrays">The arrays to be written.</param>
        /// <param name="owner">The value that owns the arrays.</param>
        /// <param name="writer">The StreamWriter to write the array values to.</param>
        private static bool WriteArrayProperties(IEnumerable<PropertyInfo> properties, object owner, StreamWriter writer)
        {
            var arrayProperties = properties.Where(p => IsArrayType(p.PropertyType, p.GetValue(owner)));
            foreach (var arrayProp in arrayProperties)
            {
                var arrayVal = (System.Collections.IEnumerable)arrayProp.GetValue(owner);
                var elementType = arrayProp.PropertyType.GetElementType();
                
                if (IsArrayType(elementType, arrayVal))
                {
                    continue;
                }

                if (!IsNativeType(elementType))
                {
                    throw new InvalidOperationException("Cannot Serialize Complex Types in an array");
                }

                bool isFirstEntry = true;

                writer.Write("{0} {1} {2}", arrayProp.Name, Toml.Parser.Tokens.ValueSeparator, Toml.Parser.Tokens.ArrayStart);
                foreach (var entry in arrayVal)
                {
                    if (!isFirstEntry)
                    {
                        writer.Write("{0} {1}", Toml.Parser.Tokens.ArraySeparator, GetNativeValueString(elementType, entry));
                    }
                    else
                    {
                        writer.Write("{0}", GetNativeValueString(elementType, entry));
                        isFirstEntry = false;
                    }
                }

                writer.WriteLine(Toml.Parser.Tokens.ArrayEnd);
            }

            return true;
        }

        /// <summary>
        /// Indicates whether the specified type can be serialized as a native Toml Type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type can be serialized as a native Toml Type, otherwise false.</returns>
        private static bool IsNativeType(Type type)
        {
            return NativeTypes.Any(kv => kv.Key == type);
        }

        /// <summary>
        /// Indicates whether or not the value of the specified type should be serialized as an array.
        /// </summary>
        private static bool IsArrayType(Type type, object value)
        {
            // we need to make sure strings don't get written as arrays
            if (IsNativeType(type))
            {
                return false;
            }

            return ((value != null) && (type.IsArray || (typeof(System.Collections.IEnumerable).IsInstanceOfType(value))));
        }

        /// <summary>
        /// Gets the string to use when writing a native type value.
        /// </summary>
        /// <param name="valueType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetNativeValueString(Type valueType, object value)
        {
            return NativeTypes.Where(kv => kv.Key == valueType)
                              .Select(kv => kv.Value(value)).First();
        }

        /// <summary>
        /// Wraps a string in quotes and adds an escape char before any escapable charaters.
        /// </summary>
        private static string EscapeAndQuoteString(string value)
        {
            return "\""
                 + value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r", "\\\r")
                        .Replace("\n", "\\\n")
                        .Replace("\f", "\\\f")
                        .Replace("\t", "\\\t")
                 + "\"";
        }
    }
}
