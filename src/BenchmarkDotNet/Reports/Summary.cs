﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Reports
{
    public class Summary
    {
        public string Title { get; }
        public BenchmarkCase[] BenchmarksCases { get; }
        public BenchmarkReport[] Reports { get; }
        public ISummaryStyle Style { get; }
        public HostEnvironmentInfo HostEnvironmentInfo { get; }
        public IConfig Config { get; }
        public string ResultsDirectoryPath { get; }
        public SummaryTable Table { get; }
        public TimeSpan TotalTime { get; }
        public ValidationError[] ValidationErrors { get; }
        public string AllRuntimes { get; }

        private readonly Dictionary<BenchmarkCase, BenchmarkReport> reportMap = new Dictionary<BenchmarkCase, BenchmarkReport>();
        private readonly IOrderer orderer;

        public bool HasReport(BenchmarkCase benchmarkCase) => reportMap.ContainsKey(benchmarkCase);

        /// <summary>
        /// Returns a report for the given benchmark or null if there is no a corresponded report.
        /// </summary>        
        public BenchmarkReport this[BenchmarkCase benchmarkCase] => reportMap.GetValueOrDefault(benchmarkCase);

        public bool HasCriticalValidationErrors => ValidationErrors.Any(validationError => validationError.IsCritical);

        [CanBeNull]
        public string GetLogicalGroupKey(BenchmarkCase benchmarkCase) => orderer.GetLogicalGroupKey(Config, BenchmarksCases, benchmarkCase);

        public int GetNumberOfExecutedBenchmarks() => Reports.Count(report => report.ExecuteResults.Any(result => result.FoundExecutable));

        public Summary(string title,
                       IList<BenchmarkReport> reports,
                       HostEnvironmentInfo hostEnvironmentInfo,
                       IConfig config, string resultsDirectoryPath,
                       TimeSpan totalTime,
                       ValidationError[] validationErrors)
            : this(title, hostEnvironmentInfo, config, resultsDirectoryPath, totalTime, validationErrors)
        {
            BenchmarksCases = reports.Select(r => r.BenchmarkCase).ToArray();
            foreach (var report in reports)
                reportMap[report.BenchmarkCase] = report;
            Reports = BenchmarksCases.Select(b => reportMap[b]).ToArray();

            orderer = config.GetOrderer() ?? DefaultOrderer.Instance;
            BenchmarksCases = orderer.GetSummaryOrder(BenchmarksCases, this).ToArray();
            Reports = BenchmarksCases.Select(b => reportMap[b]).ToArray();

            Style = config.GetSummaryStyle();
            Table = GetTable(Style);
            AllRuntimes = BuildAllRuntimes();
        }

        private Summary(string title,
                        HostEnvironmentInfo hostEnvironmentInfo,
                        IConfig config,
                        string resultsDirectoryPath,
                        TimeSpan totalTime,
                        ValidationError[] validationErrors,
                        BenchmarkCase[] benchmarksCase,
                        BenchmarkReport[] reports)
            : this(title, hostEnvironmentInfo, config, resultsDirectoryPath, totalTime, validationErrors)
        {
            BenchmarksCases = benchmarksCase;
            Table = GetTable(config.GetSummaryStyle());
            Reports = reports ?? Array.Empty<BenchmarkReport>();
        }

        private Summary(string title,
                        HostEnvironmentInfo hostEnvironmentInfo,
                        IConfig config,
                        string resultsDirectoryPath,
                        TimeSpan totalTime,
                        ValidationError[] validationErrors)
        {
            Title = title;
            HostEnvironmentInfo = hostEnvironmentInfo;
            Config = config;
            ResultsDirectoryPath = resultsDirectoryPath;
            TotalTime = totalTime;
            ValidationErrors = validationErrors;
            Reports = Array.Empty<BenchmarkReport>();
        }

        internal SummaryTable GetTable(ISummaryStyle style) => new SummaryTable(this, style);

        internal static Summary CreateFailed(BenchmarkCase[] benchmarksCase,
                                             string title,
                                             HostEnvironmentInfo hostEnvironmentInfo,
                                             IConfig config,
                                             string resultsDirectoryPath,
                                             ValidationError[] validationErrors) 
            => new Summary(title, hostEnvironmentInfo, config, resultsDirectoryPath, TimeSpan.Zero, validationErrors, benchmarksCase, Array.Empty<BenchmarkReport>());

        internal static Summary Join(List<Summary> summaries, IConfig commonSettingsConfig, ClockSpan clockSpan) 
            => new Summary(
                $"BenchmarkRun-joined-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}",
                summaries.SelectMany(summary => summary.Reports).ToArray(),
                HostEnvironmentInfo.GetCurrent(), 
                commonSettingsConfig,
                summaries.First().ResultsDirectoryPath,
                clockSpan.GetTimeSpan(),
                summaries.SelectMany(summary => summary.ValidationErrors).ToArray());

        private string BuildAllRuntimes()
        {
            var jobRuntimes = new Dictionary<string, string>(); // JobId -> Runtime
            var orderedJobs = new List<string>();

            orderedJobs.Add("[Host]");
            jobRuntimes["[Host]"] = HostEnvironmentInfo.GetRuntimeInfo();

            foreach (var benchmarkReport in Reports)
            {
                string runtime = benchmarkReport.GetRuntimeInfo();
                if (runtime != null)
                {
                    string jobId = benchmarkReport.BenchmarkCase.Job.ResolvedId;

                    if (!jobRuntimes.ContainsKey(jobId))
                    {
                        orderedJobs.Add(jobId);
                        jobRuntimes[jobId] = runtime;
                    }
                }
            }

            int jobIdMaxWidth = orderedJobs.Max(j => j.ToString().Length);

            var lines = orderedJobs.Select(jobId => $"  {jobId.PadRight(jobIdMaxWidth)} : {jobRuntimes[jobId]}");
            return string.Join(Environment.NewLine, lines);
        }
    }
}