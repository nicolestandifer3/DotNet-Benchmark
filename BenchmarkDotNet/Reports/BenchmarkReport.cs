using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<Measurement> AllRuns { get; }

        public GenerateResult GenerateResult { get; }
        public BuildResult BuildResult { get; }
        public IList<ExecuteResult> ExecuteResults { get; }

        public Statistics TargetStatistics => this.GetTargetRuns().Any() 
            ? new Statistics(this.GetTargetRuns().Select(r => r.GetAverageNanoseconds())) 
            : null;

        public BenchmarkReport(
            Benchmark benchmark,
            GenerateResult generateResult,
            BuildResult buildResult,
            IList<ExecuteResult> executeResults,
            IList<Measurement> allRuns)
        {
            Benchmark = benchmark;
            GenerateResult = generateResult;
            BuildResult = buildResult;
            ExecuteResults = executeResults;
            AllRuns = allRuns ?? new Measurement[0];
        }

        public override string ToString() => $"{Benchmark.ShortInfo}, {AllRuns.Count} runs";
    }

    public static class BenchmarkReportExtensions
    {
        public static IList<Measurement> GetTargetRuns(this BenchmarkReport report) =>
            report.AllRuns.Where(r => r.IterationMode == IterationMode.MainTarget).ToList();
    }
}