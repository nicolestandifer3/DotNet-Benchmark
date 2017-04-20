﻿using System;
using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Environments
{
    // this class is used by our auto-generated benchmark program, 
    // keep it in mind if you want to do some renaming
    // you can finde the source code at Templates\BenchmarkProgram.txt
    public class HostEnvironmentInfo : BenchmarkEnvironmentInfo
    {
        public const string BenchmarkDotNetCaption = "BenchmarkDotNet";

        public static readonly CultureInfo MainCultureInfo;

        // TODO: API to setup the logger.
        /// <summary>
        /// Logger to use when there's no config available.
        /// </summary>
        public static ILogger FallbackLogger { get; } = ConsoleLogger.Default;

        private static HostEnvironmentInfo Current;

        public string BenchmarkDotNetVersion { get; protected set; }

        /// <summary>
        /// Could be expensive
        /// </summary>
        public Lazy<string> OsVersion { get; protected set; }

        /// <summary>
        /// is expensive to call (1s)
        /// </summary>
        public Lazy<string> ProcessorName { get; protected set; }

        public int ProcessorCount { get; protected set; }

        public string JitModules { get; protected set; }

        /// <summary>
        /// is expensive to call (creates new process by calling dotnet --version)
        /// </summary>
        public Lazy<string> DotNetCliVersion { get; protected set; }

        /// <summary>
        /// The frequency of the timer as the number of ticks per second.
        /// </summary>
        public Frequency ChronometerFrequency { get; protected set; }
        public TimeInterval ChronometerResolution => ChronometerFrequency.ToResolution();

        public HardwareTimerKind HardwareTimerKind { get; protected set; }

        static HostEnvironmentInfo()
        {
            MainCultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            MainCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        protected HostEnvironmentInfo()
        {
            BenchmarkDotNetVersion = GetBenchmarkDotNetVersion();
            OsVersion = new Lazy<string>(RuntimeInformation.GetOsVersion);
            ProcessorName = new Lazy<string>(RuntimeInformation.GetProcessorName);
            ProcessorCount = Environment.ProcessorCount;
            ChronometerFrequency = Chronometer.Frequency;
            HardwareTimerKind = Chronometer.HardwareTimerKind;
            JitModules = RuntimeInformation.GetJitModulesInfo();
            DotNetCliVersion = new Lazy<string>(DotNetCliCommandExecutor.GetDotNetCliVersion);
        }

        public new static HostEnvironmentInfo GetCurrent() => Current ?? (Current = new HostEnvironmentInfo());

        public override IEnumerable<string> ToFormattedString()
        {
            yield return $"{BenchmarkDotNetCaption}=v{BenchmarkDotNetVersion}, OS={OsVersion.Value}";
            yield return $"Processor={ProcessorName.Value}, ProcessorCount={ProcessorCount}";
            yield return $"Frequency={ChronometerFrequency}, Resolution={ChronometerResolution}, Timer={HardwareTimerKind.ToString().ToUpper()}";
#if !CLASSIC
            yield return $"dotnet cli version={DotNetCliVersion.Value}";
#endif
        }

        internal bool IsDotNetCliInstalled() => !string.IsNullOrEmpty(DotNetCliVersion.Value);

        private static string GetBenchmarkDotNetVersion() => BenchmarkDotNetInfo.FullVersion;
    }
}