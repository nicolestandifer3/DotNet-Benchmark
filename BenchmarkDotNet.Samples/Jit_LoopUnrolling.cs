﻿using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples
{
    [Task(platform: BenchmarkPlatform.X86, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit)]
    [Task(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Jit_LoopUnrolling
    {
        [Benchmark]
        public int Sum()
        {
            int sum = 0;
            for (int i = 0; i < 1024; i++)
                sum += i;
            return sum;
        }
    }
}