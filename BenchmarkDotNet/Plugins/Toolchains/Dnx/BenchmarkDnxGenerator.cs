﻿using System;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains.Classic;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    internal class BenchmarkDnxGenerator : BenchmarkClassicGenerator
    {
        internal const string ProjectFileName = "project.json";

        public BenchmarkDnxGenerator(IBenchmarkLogger logger) : base(logger)
        {
        }

        protected override void GenerateProjectFile(string projectDir, Benchmark benchmark)
        {
            var template = ResourceHelper.LoadTemplate("BenchmarkProject.json");

            var content = SetPlatform(template, benchmark.Task.Configuration.Platform);
            content = SetDependency(content, benchmark.Target.Type);
    
            var projectJsonFilePath = Path.Combine(projectDir, ProjectFileName);

            File.WriteAllText(projectJsonFilePath, content);
        }

        protected override void GenerateProjectBuildFile(string projectDir)
        {
            // do nothing on purpose, we do not need bat file
        }

        private static string SetPlatform(string template, BenchmarkPlatform platform)
        {
            return template.Replace("$PLATFORM", platform.ToConfig()); // todo: verify name
        }

        private static string SetDependency(string template, Type benchmarkTarget)
        {
            var assemblyName = benchmarkTarget.Assembly.GetName();

            var packageVersion =
                $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}-*";

            return template
                .Replace("$EXECUTINGASSEMBLYVERSION", packageVersion) 
                .Replace("$EXECUTINGASSEMBLY", assemblyName.Name);
        }
    }
}