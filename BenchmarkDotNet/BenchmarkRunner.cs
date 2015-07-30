﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;
using Microsoft.Build.Execution;
using Microsoft.CSharp;

namespace BenchmarkDotNet
{
    public class BenchmarkRunner
    {
        public BenchmarkRunner(IEnumerable<IBenchmarkLogger> loggers)
        {
            Logger = new BenchmarkCompositeLogger(loggers.ToArray());
        }

        public BenchmarkRunner() : this(new[] { new BenchmarkConsoleLogger() })
        {
        }

        public IBenchmarkLogger Logger { get; }

        private readonly BenchmarkProjectGenerator benchmarkProjectGenerator = new BenchmarkProjectGenerator();

        public IEnumerable<BenchmarkReport> RunCompetition(object benchmarkCompetition, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(CompetitionToBenchmarks(benchmarkCompetition, defaultSettings).ToList());
        }

        public IEnumerable<BenchmarkReport> RunUrl(string url, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(UrlToBenchmarks(url, defaultSettings).ToList());
        }

        public IEnumerable<BenchmarkReport> RunCompetition(List<Benchmark> benchmarks)
        {
            benchmarks.Sort((a, b) => string.Compare((a.Task.Configuration.Caption + a.Target.Caption), b.Task.Configuration.Caption + b.Target.Caption, StringComparison.Ordinal));
            Logger.WriteLineHeader("// ***** Competition: Start   *****");
            Logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                Logger.WriteLineInfo($"//   {benchmark.Caption} {benchmark.Task.Settings.ToArgs()}");
            Logger.NewLine();

            var importantPropertyNames = benchmarks.Select(b => b.Properties).GetImportantNames();

            var reports = new List<BenchmarkReport>();
            foreach (var benchmark in benchmarks)
            {
                var report = Run(benchmark, importantPropertyNames);
                reports.Add(report);
                if (report.Runs.Count > 0)
                {
                    var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                    Logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                    Logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                }
                Logger.NewLine();
            }
            Logger.WriteLineHeader("// ***** Competition: Finish  *****");
            Logger.NewLine();
            Logger.WriteLineInfo("```ini");
            Logger.WriteLineInfo(EnvironmentHelper.GetFullEnvironmentInfo("Host", false));
            var reportStats = reports.Where(r => r.Runs.Count > 0).Select(
                r => new
                {
                    r.Benchmark,
                    Stat = new BenchmarkRunReportsStatistic("Target", r.Runs)
                }).ToList();
            var table = new List<string[]> { new[] { "Type", "Method", "Mode", "Platform", "Jit", ".NET", "AvrTime", "StdDev", "op/s" } };
            foreach (var reportStat in reportStats)
            {
                var b = reportStat.Benchmark;

                // Ops/sec number formatting:
                //      - Thousand separators: we generally expect large numbers so these 
                //        would make it easier to view.
                //      - Decimals: Do we really need these? Perhaps we do but only if we have
                //        really small values to deal with.
                // In any case, we would like to have all numbers to be aligned, ideally by decimal point
                // but I'm too lazy to do that now, so maybe a compomise of fixed two decimals would do at the mo.
                //
                // Hence the choice of {N2} formatting string.
                var opsPerSecValue = string.Format(EnvironmentHelper.MainCultureInfo, "{0:N2}", reportStat.Stat.OperationsPerSeconds.Median);

                string[] row = {
                    b.Target.Type.Name,
                    b.Target.Method.Name,
                    b.Task.Configuration.Mode.ToString(),
                    b.Task.Configuration.Platform.ToString(),
                    b.Task.Configuration.JitVersion.ToString(),
                    b.Task.Configuration.Framework.ToString(),
                    new BenchmarkTimeSpan(reportStat.Stat.AverageTime.Median).ToString(),
                    new BenchmarkTimeSpan(reportStat.Stat.AverageTime.StandardDeviation).ToString(),
                    opsPerSecValue
                };
                table.Add(row);
            }
            PrintTable(table);
            var benchmarksWithTroubles = reports.Where(r => r.Runs.Count == 0).Select(r => r.Benchmark).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                Logger.NewLine();
                Logger.WriteLineError("Benchmarks with troubles:");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    Logger.WriteLineError("  " + benchmarkWithTroubles.Caption);
            }
            Logger.NewLine();
            Logger.WriteLineHeader("// ***** Competition: End *****");
            return reports;
        }

        private void PrintTable(List<string[]> table)
        {
            int rowCount = table.Count, colCount = table[0].Length;
            int[] widths = new int[colCount];
            bool[] areSame = new bool[colCount];
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                areSame[colIndex] = rowCount > 2 && colIndex < colCount - 3;
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    widths[colIndex] = Math.Max(widths[colIndex], table[rowIndex][colIndex].Length + 1);
                    if (rowIndex > 1 && table[rowIndex][colIndex] != table[1][colIndex])
                        areSame[colIndex] = false;
                }
            }
            if (areSame.Any(s => s))
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (areSame[colIndex])
                        Logger.WriteInfo($"{table[0][colIndex]}={table[1][colIndex]}  ");
                Logger.NewLine();
                Logger.WriteLineInfo("```");
                Logger.NewLine();
            }

            table.Insert(1, widths.Select(w => new string('-', w)).ToArray());
            foreach (var row in table)
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (!areSame[colIndex])
                        Logger.WriteStatistic(row[colIndex].PadLeft(widths[colIndex], ' ') + " |");
                Logger.NewLine();
            }
        }

        public BenchmarkReport Run(Benchmark benchmark, IList<string> importantPropertyNames)
        {
            Logger.WriteLineHeader("// **************************");
            Logger.WriteLineHeader("// Benchmark: " + benchmark.Description);
            var directoryPath = benchmarkProjectGenerator.GenerateProject(benchmark);
            Logger.WriteLineInfo("// Generated project: " + directoryPath);
            Logger.NewLine();
            Logger.WriteLineInfo("// Build:");
            var buildResult = benchmarkProjectGenerator.BuildProject(directoryPath);
            if (buildResult.OverallResult == BuildResultCode.Success)
                Logger.WriteLineInfo("// OverallResult = Success");
            else
            {
                Logger.WriteLineError("// OverallResult = Failure");
                return new BenchmarkReport(benchmark, new BenchmarkRunReport[0]);
            }
            Logger.NewLine();
            var processCount = Math.Max(1, benchmark.Task.ProcessCount);
            var runReports = new List<BenchmarkRunReport>();
            var exeFileName = Path.Combine(directoryPath, "Program.exe");
            for (int processNumber = 0; processNumber < processCount; processNumber++)
            {
                Logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {processCount}");
                if (importantPropertyNames.Any())
                {
                    Logger.WriteInfo("// ");
                    foreach (var name in importantPropertyNames)
                        Logger.WriteInfo($"{name}={benchmark.Properties.GetValue(name)} ");
                    Logger.NewLine();
                }

                var executor = new BenchmarkExecutor(Logger);
                if (File.Exists(exeFileName))
                {
                    var lines = executor.Exec(exeFileName, benchmark.Task.Settings.ToArgs());
                    var iterRunReports = lines.Select(line => BenchmarkRunReport.Parse(Logger, line)).Where(r => r != null).ToList();
                    runReports.AddRange(iterRunReports);
                }
            }
            Logger.NewLine();
            return new BenchmarkReport(benchmark, runReports);
        }

        private static IEnumerable<Benchmark> CompetitionToBenchmarks(object competition, BenchmarkSettings defaultSettings)
        {
            if (defaultSettings == null)
                defaultSettings = BenchmarkSettings.CreateDefault();
            var targetType = competition.GetType();
            var methods = targetType.GetMethods();
            for (int i = 0; i < methods.Length; i++)
            {
                var methodInfo = methods[i];
                var benchmarkAttribute = methodInfo.ResolveAttribute<BenchmarkAttribute>();
                if (benchmarkAttribute != null)
                {
                    var target = new BenchmarkTarget(targetType, methodInfo, benchmarkAttribute.Description);
                    AssertBenchmarkMethodHasCorrectSignature(methodInfo);
                    AssertBenchmarkMethodIsAccessible(methodInfo);
                    AssertBenchmarkMethodIsNotDeclaredInGeneric(methodInfo);
                    AssertBenchmarkMethodIsNotGeneric(methodInfo);
                    foreach (var task in BenchmarkTask.Resolve(methodInfo, defaultSettings))
                        yield return new Benchmark(target, task);
                }
            }
        }

        private static IEnumerable<Benchmark> UrlToBenchmarks(string url, BenchmarkSettings defaultSettings)
        {
            string benchmarkContent = String.Empty;
            try
            {
                var webRequest = WebRequest.Create(url);
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                    benchmarkContent = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(benchmarkContent))
                {
                    Console.WriteLine($"content of '{url}' is empty.");
                    yield break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                yield break;
            }
            var cSharpCodeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll" }) { CompilerOptions = "/unsafe" };
            compilerParameters.ReferencedAssemblies.Add(typeof(BenchmarkRunner).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText));
                yield break;
            }
            foreach (var type in compilerResults.CompiledAssembly.GetTypes())
            {
                var instance = Activator.CreateInstance(type);
                foreach (var benchmark in CompetitionToBenchmarks(instance, defaultSettings))
                    yield return new Benchmark(new BenchmarkTarget(benchmark.Target.Type, benchmark.Target.Method, benchmark.Target.Description, benchmarkContent), benchmark.Task);
            }
        }

        private static void AssertBenchmarkMethodHasCorrectSignature(MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any())
                throw new InvalidOperationException($"Benchmark method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        private static void AssertBenchmarkMethodIsAccessible(MethodInfo methodInfo)
        {
            if (!methodInfo.IsPublic)
                throw new InvalidOperationException($"Benchmark method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public.");

            var declaringType = methodInfo.DeclaringType;

            while (declaringType != null)
            {
                if (!declaringType.IsPublic && !declaringType.IsNestedPublic)
                    throw new InvalidOperationException($"Benchmark method {methodInfo.Name} defined within type {declaringType.FullName} has incorrect access modifiers.\nDeclaring type must be public.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertBenchmarkMethodIsNotDeclaredInGeneric(MethodInfo methodInfo)
        {
            var declaringType = methodInfo.DeclaringType;

            while (declaringType != null)
            {
                if (declaringType.IsGenericType)
                    throw new InvalidOperationException($"Benchmark method {methodInfo.Name} defined within generic type {declaringType.FullName}.\nBenchmark methods in generic types are not supported.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertBenchmarkMethodIsNotGeneric(MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new InvalidOperationException($"Benchmark method {methodInfo.Name} is generic.\nGeneric benchmark methods are not supported.");
        }
    }
}