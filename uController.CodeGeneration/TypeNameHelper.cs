// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Extensions.Internal
{
    internal static class TypeNameHelper
    {
        private const char DefaultNestedTypeDelimiter = '+';

        private static readonly Dictionary<string, string> _builtInTypeNames = new Dictionary<string, string>
        {
            { typeof(void).FullName, "void" },
            { typeof(bool).FullName, "bool" },
            { typeof(byte).FullName, "byte" },
            { typeof(char).FullName, "char" },
            { typeof(decimal).FullName, "decimal" },
            { typeof(double).FullName, "double" },
            { typeof(float).FullName, "float" },
            { typeof(int).FullName, "int" },
            { typeof(long).FullName, "long" },
            { typeof(object).FullName, "object" },
            { typeof(sbyte).FullName, "sbyte" },
            { typeof(short).FullName, "short" },
            { typeof(string).FullName, "string" },
            { typeof(uint).FullName, "uint" },
            { typeof(ulong).FullName, "ulong" },
            { typeof(ushort).FullName, "ushort" }
        };

        public static string GetTypeDisplayName(object item, bool fullName = true)
        {
            return item == null ? null : GetTypeDisplayName(item.GetType(), fullName);
        }

        /// <summary>
        /// Pretty print a type name.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="fullName"><c>true</c> to print a fully qualified name.</param>
        /// <param name="includeGenericParameterNames"><c>true</c> to include generic parameter names.</param>
        /// <param name="includeGenericParameters"><c>true</c> to include generic parameters.</param>
        /// <param name="nestedTypeDelimiter">Character to use as a delimiter in nested type names</param>
        /// <returns>The pretty printed type name.</returns>
        public static string GetTypeDisplayName(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = DefaultNestedTypeDelimiter)
        {
            var builder = new StringBuilder();
            ProcessType(builder, type, new DisplayNameOptions(fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter));
            return builder.ToString();
        }

        private static void ProcessType(StringBuilder builder, Type type, in DisplayNameOptions options)
        {
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                ProcessGenericType(builder, type, genericArguments, genericArguments.Length, options);
            }
            else if (type.IsArray)
            {
                ProcessArrayType(builder, type, options);
            }
            else if (_builtInTypeNames.TryGetValue(type.FullName, out var builtInName))
            {
                builder.Append(builtInName);
            }
            else if (type.IsGenericParameter)
            {
                if (options.IncludeGenericParameterNames)
                {
                    builder.Append(type.Name);
                }
            }
            else
            {
                var name = options.FullName ? type.FullName : type.Name;
                builder.Append(name);

                if (options.NestedTypeDelimiter != DefaultNestedTypeDelimiter)
                {
                    builder.Replace(DefaultNestedTypeDelimiter, options.NestedTypeDelimiter, builder.Length - name.Length, name.Length);
                }
            }
        }

        private static void ProcessArrayType(StringBuilder builder, Type type, in DisplayNameOptions options)
        {
            var innerType = type;
            while (innerType.IsArray)
            {
                innerType = innerType.GetElementType();
            }

            ProcessType(builder, innerType, options);

            while (type.IsArray)
            {
                builder.Append('[');
                builder.Append(',', type.GetArrayRank() - 1);
                builder.Append(']');
                type = type.GetElementType();
            }
        }

        private static void ProcessGenericType(StringBuilder builder, Type type, Type[] genericArguments, int length, in DisplayNameOptions options)
        {
            var offset = 0;
            if (type.IsNested)
            {
                offset = type.DeclaringType.GetGenericArguments().Length;
            }

            if (options.FullName)
            {
                if (type.IsNested)
                {
                    ProcessGenericType(builder, type.DeclaringType, genericArguments, offset, options);
                    builder.Append(options.NestedTypeDelimiter);
                }
                else if (!string.IsNullOrEmpty(type.Namespace))
                {
                    builder.Append(type.Namespace);
                    builder.Append('.');
                }
            }

            var genericPartIndex = type.Name.IndexOf('`');
            if (genericPartIndex <= 0)
            {
                builder.Append(type.Name);
                return;
            }

            builder.Append(type.Name, 0, genericPartIndex);

            if (options.IncludeGenericParameters)
            {
                builder.Append('<');
                for (var i = offset; i < length; i++)
                {
                    ProcessType(builder, genericArguments[i], options);
                    if (i + 1 == length)
                    {
                        continue;
                    }

                    builder.Append(',');
                    if (options.IncludeGenericParameterNames || !genericArguments[i + 1].IsGenericParameter)
                    {
                        builder.Append(' ');
                    }
                }
                builder.Append('>');
            }
        }

        private readonly struct DisplayNameOptions
        {
            public DisplayNameOptions(bool fullName, bool includeGenericParameterNames, bool includeGenericParameters, char nestedTypeDelimiter)
            {
                FullName = fullName;
                IncludeGenericParameters = includeGenericParameters;
                IncludeGenericParameterNames = includeGenericParameterNames;
                NestedTypeDelimiter = nestedTypeDelimiter;
            }

            public bool FullName { get; }

            public bool IncludeGenericParameters { get; }

            public bool IncludeGenericParameterNames { get; }

            public char NestedTypeDelimiter { get; }
        }
    }
}