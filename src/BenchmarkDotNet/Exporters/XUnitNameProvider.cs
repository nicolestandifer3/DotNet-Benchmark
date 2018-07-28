﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    internal class XUnitNameProvider 
    {
        private static readonly IReadOnlyDictionary<Type, string> Aliases = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(byte?), "byte?" },
            { typeof(sbyte?), "sbyte?" },
            { typeof(short?), "short?" },
            { typeof(ushort?), "ushort?" },
            { typeof(int?), "int?" },
            { typeof(uint?), "uint?" },
            { typeof(long?), "long?" },
            { typeof(ulong?), "ulong?" },
            { typeof(float?), "float?" },
            { typeof(double?), "double?" },
            { typeof(decimal?), "decimal?" },
            { typeof(bool?), "bool?" },
            { typeof(char?), "char?" }
        };
        
        internal static string GetBenchmarkName(BenchmarkCase benchmarkCase)
        {
            var type = benchmarkCase.Descriptor.Type;
            var method = benchmarkCase.Descriptor.WorkloadMethod;

            // we can't just use type.FullName because we need sth different for generics (it reports SimpleGeneric`1[[System.Int32, mscorlib, Version=4.0.0.0)
            var name = new StringBuilder();

            if (!string.IsNullOrEmpty(type.Namespace))
                name.Append(type.Namespace).Append('.');

            name.Append(GetNestedTypes(type));

            name.Append(GetTypeName(type)).Append('.');

            name.Append(method.Name);

            if (benchmarkCase.HasParameters)
                name.Append(GetBenchmarkParameters(method, benchmarkCase.Parameters));

            return name.ToString();
        }

        private static string GetNestedTypes(Type type)
        {
            string nestedTypes = "";
            Type child = type, parent = type.DeclaringType;
            while (child.IsNested && parent != null)
            {
                nestedTypes = parent.Name + "+" + nestedTypes;

                child = parent;
                parent = parent.DeclaringType;
            }

            return nestedTypes;
        }

        private static string GetTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            string mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
            string args = string.Join(", ", type.GetGenericArguments().Select(GetTypeName).ToArray());

            return $"{mainName}<{args}>";
        }

        private static string GetBenchmarkParameters(MethodInfo method, ParameterInstances benchmarkParameters)
        {
            var methodArguments = method.GetParameters();
            var benchmarkParams = benchmarkParameters.Items.Where(parameter => !parameter.IsArgument).ToArray();
            var parametersBuilder = new StringBuilder(methodArguments.Length * 20).Append('(');

            for (int i = 0; i < methodArguments.Length; i++)
            {
                if (i > 0)
                    parametersBuilder.Append(", ");

                parametersBuilder.Append(methodArguments[i].Name).Append(':').Append(' ');
                parametersBuilder.Append(GetArgument(benchmarkParameters.GetArgument(methodArguments[i].Name).Value, methodArguments[i].ParameterType));
            }
            
            for (int i = 0; i < benchmarkParams.Length; i++)
            {
                var parameter = benchmarkParams[i];
                
                if (methodArguments.Length > 0 || i > 0)
                    parametersBuilder.Append(", ");
                
                parametersBuilder.Append(parameter.Name).Append(':').Append(' ');
                parametersBuilder.Append(GetArgument(parameter.Value, parameter.Value?.GetType()));
            }

            return parametersBuilder.Append(')').ToString();
        }

        private static string GetArgument(object argumentValue, Type argumentType)
        {
            if (argumentValue == null)
                return "null";

            if (argumentValue is IParam iparam)
                return GetArgument(iparam.Value, argumentType);
            
            if (argumentValue is object[] array && array.Length == 1)
                return GetArgument(array[0], argumentType);

            if (argumentValue is string text)
                return $"\"{EscapeWhitespaces(text)}\"";
            if (argumentValue is char character)
                return $"'{character}'";
            if (argumentValue is DateTime time)
                return time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");
            if (argumentValue is Type type)
                return $"typeof({GetTypeArgumentName(type)})";

            if (argumentType != null && argumentType.IsArray)
                return GetArray((IEnumerable)argumentValue);

            return argumentValue.ToString();
        }

        // it's not generic so I can't simply use .Skip and all other LINQ goodness
        private static string GetArray(IEnumerable collection)
        {
            var buffer = new StringBuilder().Append('[');

            int index = 0;
            foreach (var item in collection)
            {
                if (index > 0)
                    buffer.Append(", ");

                if (index > 4)
                {
                    buffer.Append("..."); // [0, 1, 2, 3, 4, ...]
                    break;
                }

                buffer.Append(GetArgument(item, item?.GetType()));

                ++index;
            }

            buffer.Append(']');

            return buffer.ToString();
        }

        private static string EscapeWhitespaces(string text)
            => text.Replace("\t", "\\t")
                   .Replace("\r\n", "\\r\\n");

        private static string GetTypeArgumentName(Type type)
        {
            if (Aliases.TryGetValue(type, out string alias))
                return alias;

            if (type.IsNullable())
                return $"{GetTypeArgumentName(Nullable.GetUnderlyingType(type))}?";
            
            if (!string.IsNullOrEmpty(type.Namespace))
                return $"{type.Namespace}.{GetTypeName(type)}";

            return GetTypeName(type);
        }
    }
}
