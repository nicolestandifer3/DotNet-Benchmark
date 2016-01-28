﻿using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Extensions
{
    internal static class ConfigurationExtensions
    {
        public static string ToConfig(this BenchmarkPlatform platform)
        {
            switch (platform)
            {
                case BenchmarkPlatform.AnyCpu:
                    return "AnyCPU";
                case BenchmarkPlatform.X86:
                    return "x86";
                case BenchmarkPlatform.X64:
                    return "x64";
                case BenchmarkPlatform.HostPlatform:
                    return IntPtr.Size == 4 ? "x86" : "x64";
                default:
                    return "AnyCPU";
            }
        }

        public static string ToConfig(this BenchmarkFramework framework, Type benchmarkType)
        {
            if (framework == BenchmarkFramework.HostFramework)
                return DetectCurrentFramework(benchmarkType);
            var number = framework.ToString().Substring(1);
            var numberArray = number.ToCharArray().Select(c => c.ToString()).ToArray();
            return "v" + string.Join(".", numberArray);
        }

        private static string DetectCurrentFramework(Type benchmarkType)
        {
            var attributes = benchmarkType.Assembly.GetCustomAttributes(false).OfType<Attribute>();
            var frameworkAttribute = attributes.FirstOrDefault(a => a.ToString() == @"System.Runtime.Versioning.TargetFrameworkAttribute");
            if (frameworkAttribute == null)
#if DEBUG
                return "v4.0"; // otherwise msbuild fails when debugging in new way from VS
#else
                return "v3.5";
#endif
            var frameworkName = frameworkAttribute.GetType()
                .GetProperty("FrameworkName", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(frameworkAttribute, null)?.ToString();
            if (frameworkName != null)
                frameworkName = frameworkName.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "";
            return frameworkName;
        }

        public static string ToConfig(this BenchmarkJitVersion jitVersion)
        {
            return jitVersion == BenchmarkJitVersion.LegacyJit ? "1" : "0";
        }
    }
}