﻿using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Roslyn;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class ConfigParserTests
    {
        public ITestOutputHelper Output { get; }

        public ConfigParserTests(ITestOutputHelper output) => Output = output;

        [Theory]
        [InlineData("--job=dry", "--exporters", "html", "rplot")]
        [InlineData("--JOB=dry", "--EXPORTERS", "html", "rplot")] // case insensitive
        [InlineData("-j", "dry", "-e", "html", "rplot")] // alias
        public void SimpleConfigParsedCorrectly(params string[] args)
        {
            var config = ConfigParser.Parse(args, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());

            Assert.Equal(3, config.GetExporters().Count()); // rplot must come together with CsvMeasurementsExporter
            Assert.Contains(HtmlExporter.Default, config.GetExporters());
            Assert.Contains(RPlotExporter.Default, config.GetExporters());
            Assert.Contains(CsvMeasurementsExporter.Default, config.GetExporters());

            Assert.Empty(config.GetColumnProviders());
            Assert.Empty(config.GetDiagnosers());
            Assert.Empty(config.GetAnalysers());
            Assert.Empty(config.GetLoggers());
        }

        [Fact]
        public void SimpleConfigAlternativeVersionParsedCorrectly()
        {
            var config = ConfigParser.Parse(new[] { "--job=Dry" }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Contains(Job.Dry, config.GetJobs());
        }

        [Fact]
        public void UnknownConfigMeansFailure()
        {
            Assert.False(ConfigParser.Parse(new[] { "--unknown" }, new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void EmptyArgsMeansConfigWithoutJobs()
        {
            var config = ConfigParser.Parse(Array.Empty<string>(), new OutputLogger(Output)).config;

            Assert.Empty(config.GetJobs());
        }

        [Fact]
        public void NonExistingPathMeansFailure()
        {
            string nonExistingFile = Path.Combine(Path.GetTempPath(), "veryUniqueFileName.exe");

            Assert.False(ConfigParser.Parse(new[] { "--cli", nonExistingFile }, new OutputLogger(Output)).isSuccess);
            Assert.False(ConfigParser.Parse(new[] { "--coreRun", nonExistingFile }, new OutputLogger(Output)).isSuccess);
        }

        [Fact]
        public void CoreRunConfigParsedCorrectly()
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var fakeCoreRunPath = typeof(ConfigParserTests).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "--job=Dry", "--coreRun", fakeCoreRunPath, "--cli", fakeDotnetCliPath }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CoreRunToolchain toolchain = config.GetJobs().Single().GetToolchain() as CoreRunToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(fakeCoreRunPath, toolchain.SourceCoreRun.FullName);
            Assert.Equal(fakeDotnetCliPath, toolchain.CustomDotNetCliPath.FullName);
        }
        
        [Fact]
        public void MonoPathParsedCorrectly()
        {
            var fakeMonoPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "-r", "mono", "--monoPath", fakeMonoPath }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is MonoRuntime mono && mono.CustomPath == fakeMonoPath));
        }
        
        [Fact]
        public void ClrVersionParsedCorrectly()
        {
            const string clrVersion = "secret";
            var config = ConfigParser.Parse(new[] { "--clrVersion", clrVersion }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is ClrRuntime clr && clr.Version == clrVersion));
        }
        
        [Fact]
        public void CoreRtPathParsedCorrectly()
        {
            var fakeCoreRtPath =  new FileInfo(typeof(ConfigParserTests).Assembly.Location).Directory;
            var config = ConfigParser.Parse(new[] { "-r", "corert", "--ilcPath", fakeCoreRtPath.FullName }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CoreRtToolchain toolchain = config.GetJobs().Single().GetToolchain() as CoreRtToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(fakeCoreRtPath.FullName, ((Publisher)toolchain.Builder).IlcPath);
        }
        
        [Theory]
        [InlineData("netcoreapp2.0")]
        [InlineData("netcoreapp2.1")]
        [InlineData("netcoreapp2.2")]
        [InlineData("netcoreapp3.0")]
        public void DotNetCliParsedCorrectly(string tfm)
        {
            var fakeDotnetCliPath = typeof(object).Assembly.Location;
            var config = ConfigParser.Parse(new[] { "-r", tfm, "--cli", fakeDotnetCliPath }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjCoreToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjCoreToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
            Assert.Equal(fakeDotnetCliPath, toolchain.CustomDotNetCliPath);
        }
        
        [Theory]
        [InlineData("net46")]
        [InlineData("net461")]
        [InlineData("net462")]
        [InlineData("net47")]
        [InlineData("net471")]
        [InlineData("net472")]
        public void NetFrameworkMonikerParsedCorrectly(string tfm)
        {
            var config = ConfigParser.Parse(new[] { "-r", tfm }, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs());
            CsProjClassicNetToolchain toolchain = config.GetJobs().Single().GetToolchain() as CsProjClassicNetToolchain;
            Assert.NotNull(toolchain);
            Assert.Equal(tfm, ((DotNetCliGenerator)toolchain.Generator).TargetFrameworkMoniker);
        }
        
        [Fact]
        public void CanCompreFewDifferentRuntimes()
        {
            var config = ConfigParser.Parse(new[] { "--runtimes", "net46", "MONO", "netcoreapp3.0", "CoreRT"}, new OutputLogger(Output)).config;

            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is ClrRuntime));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is MonoRuntime));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is CoreRtRuntime));
            Assert.Single(config.GetJobs().Where(job => job.Environment.Runtime is CoreRtRuntime));
        }
    }
}