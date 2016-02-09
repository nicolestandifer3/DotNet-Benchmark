﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Extensions;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ProcessorArchitectureTest
    {
        private class PlatformConfig : ManualConfig
        {
            public PlatformConfig(Platform platform)
            {
                Add(Job.Dry.With(platform).With(Jit.Host));
            }
        }

        const string X86FailedCaption = "x86FAILED";
        const string X64FailedCaption = "x64FAILED";
        const string AnyCpuOkCaption = "AnyCpuOkCaption";
        const string HostPlatformOkCaption = "HostPlatformOkCaption";
        const string BenchmarkNotFound = "There are no benchmarks found";

        [Fact]
        public void SpecifiedProccesorArchitectureMustBeRespected()
        {
            Verify(Platform.X86, typeof(X86Benchmark), X86FailedCaption);
            Verify(Platform.X64, typeof(X64Benchmark), X64FailedCaption);
            Verify(Platform.AnyCpu, typeof(AnyCpuBenchmark), "nvm");
            Verify(Platform.Host, typeof(HostBenchmark), "nvm");
        }

        private void Verify(Platform platform, Type benchmark, string failureText)
        {
            var logger = new AccumulationLogger();
            var config = new PlatformConfig(platform).With(logger);

            BenchmarkRunner.Run(benchmark, config);
            var testLog = logger.GetLog();

            Assert.DoesNotContain(failureText, testLog);
            Assert.DoesNotContain(BenchmarkNotFound, testLog);
        }

        public class X86Benchmark
        {
            [Benchmark]
            public void _32Bit()
            {
                if (IntPtr.Size != 4)
                {
                    throw new InvalidOperationException(X86FailedCaption);
                }
            }
        }

        public class X64Benchmark
        {
            [Benchmark]
            public void _64Bit()
            {
                if (IntPtr.Size != 8)
                {
                    throw new InvalidOperationException(X64FailedCaption);
                }
            }
        }

        public class AnyCpuBenchmark
        {
            [Benchmark]
            public void AnyCpu()
            {
                Console.WriteLine(AnyCpuOkCaption);
            }
        }

        public class HostBenchmark
        {
            [Benchmark]
            public void Host()
            {
                Console.WriteLine(HostPlatformOkCaption);
            }
        }
    }
}