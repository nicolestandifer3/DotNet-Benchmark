﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        internal static T ResolveAttribute<T>(this Type type) where T : Attribute =>
            type?.GetTypeInfo().GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();

        internal static T ResolveAttribute<T>(this MethodInfo methodInfo) where T : Attribute =>
            methodInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static T ResolveAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute =>
            propertyInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static T ResolveAttribute<T>(this FieldInfo fieldInfo) where T : Attribute =>
            fieldInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static bool HasAttribute<T>(this MethodInfo methodInfo) where T : Attribute =>
            methodInfo.ResolveAttribute<T>() != null;

        internal static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

        /// <summary>
        /// returns type name which can be used in generated C# code
        /// </summary>
        internal static string GetCorrectCSharpTypeName(this Type type)
        {
            if (NeedsUglyHackForByGenericByRefTypes(type))
                return UglyHack(type);

            if (type == typeof(void))
                return "void";
            var prefix = "";
            if (!string.IsNullOrEmpty(type.Namespace))
                prefix += type.Namespace + ".";

            var nestedTypes = "";
            Type child = type, parent = type.DeclaringType;
            while (child.IsNested && parent != null)
            {
                nestedTypes = parent.Name + "." + nestedTypes;

                child = parent;
                parent = parent.DeclaringType;
            }
            prefix += nestedTypes;
                

            if (type.GetTypeInfo().IsGenericParameter)
                return type.Name.Replace("&", string.Empty);
            if (type.GetTypeInfo().IsGenericType)
            {
                var mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
                string args = string.Join(", ", type.GetGenericArguments().Select(GetCorrectCSharpTypeName).ToArray());
                return $"{prefix}{mainName}<{args}>";
            }

            if (type.IsArray)
                return GetCorrectCSharpTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

            return prefix + type.Name;
        }

        /// <summary>
        /// returns simple, human friendly display name
        /// </summary>
        internal static string GetDisplayName(this Type type) => GetDisplayName(type.GetTypeInfo());

        /// <summary>
        /// returns simple, human friendly display name
        /// </summary>
        internal static string GetDisplayName(this TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericType)
                return typeInfo.Name;

            var mainName = typeInfo.Name.Substring(0, typeInfo.Name.IndexOf('`'));
            string args = string.Join(", ", typeInfo.GetGenericArguments().Select(GetDisplayName).ToArray());
            return $"{mainName}<{args}>";
        }

        internal static IEnumerable<MethodInfo> GetAllMethods(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var methodInfo in typeInfo.DeclaredMethods)
                    yield return methodInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var fieldInfo in typeInfo.DeclaredFields)
                    yield return fieldInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var propertyInfo in typeInfo.DeclaredProperties)
                    yield return propertyInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static Type[] GetRunnableBenchmarks(this Assembly assembly)
            => assembly
                .GetTypes()
                .Where(type => type.ContainsRunnableBenchmarks())
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();

        internal static bool ContainsRunnableBenchmarks(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            
            if (typeInfo.IsAbstract 
                || typeInfo.IsSealed 
                || typeInfo.IsNotPublic 
                || (typeInfo.IsGenericType && !IsRunnableGenericType(typeInfo)))
                return false;

            return typeInfo.GetBenchmarks().Any();
        }

        internal static MethodInfo[] GetBenchmarks(this TypeInfo typeInfo)
            => typeInfo
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any())
                .ToArray();

        internal static (string Name, TAttribute Attribute, bool IsPrivate, bool IsStatic, Type ParameterType)[] GetTypeMembersWithGivenAttribute<TAttribute>(this Type type, BindingFlags reflectionFlags)
            where TAttribute : Attribute
        {
            var allFields = type.GetFields(reflectionFlags)
                                .Select(f => (
                                    Name: f.Name, 
                                    Attribute: f.ResolveAttribute<TAttribute>(),
                                    IsPrivate: f.IsPrivate,
                                    IsStatic: f.IsStatic, 
                                    ParameterType: f.FieldType));

            var allProperties = type.GetProperties(reflectionFlags)
                                    .Select(p => (
                                        Name: p.Name, 
                                        Attribute: p.ResolveAttribute<TAttribute>(), 
                                        IsPrivate: p.GetSetMethod() == null, 
                                        IsStatic: p.GetSetMethod() != null && p.GetSetMethod().IsStatic, 
                                        PropertyType: p.PropertyType));

            var joined = allFields.Concat(allProperties).Where(member => member.Attribute != null).ToArray();

            foreach (var member in joined.Where(m => m.IsPrivate))
                throw new InvalidOperationException($"Member \"{member.Name}\" must be public if it has the [{typeof(TAttribute).Name}] attribute applied to it");

            return joined;
        }

        private static bool IsRunnableGenericType(TypeInfo typeInfo)
            => // if it is an open generic - there must be GenericBenchmark attributes
                (!typeInfo.IsGenericTypeDefinition || (typeInfo.GenericTypeArguments.Any() || typeInfo.GetCustomAttributes(true).OfType<GenericTypeArgumentsAttribute>().Any()))
                    && typeInfo.DeclaredConstructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0); // we need public parameterless ctor to create it       

        private static bool NeedsUglyHackForByGenericByRefTypes(Type type)
        {
            // the reflection is missing information about types passed by ref (ie ref ValuTuple<int> is reported as NON generic type)
            // more info https://github.com/dotnet/corefx/issues/29975

            return type.IsByRef && !type.IsGenericType && type.Name.Contains('`');
        }

        private static string UglyHack(Type byRefGeneric) // I hate myslef for writing this piece of crap
        {
            // it is sth like System.ValueTuple`2[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int16, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]&
            var fullName = byRefGeneric.FullName;
            var mainName = fullName.Substring(0, fullName.IndexOf('`'));
            var arguments = fullName.Split('[')
                .Skip(2) // System.ValueTuple`2[
                .Select(argFullName => argFullName.Substring(0, argFullName.IndexOf(',')))
                .Select(argumentName => argumentName.Replace('+', '.')) // for nested things...
                .ToArray();

            return $"{mainName}<{string.Join(", ", arguments)}>";
        }
    }
}