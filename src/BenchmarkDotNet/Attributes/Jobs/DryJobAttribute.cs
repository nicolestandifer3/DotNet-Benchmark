﻿using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class DryJobAttribute : JobConfigBaseAttribute
    {
        public DryJobAttribute() : base(Job.Dry)
        {
        }

        /// <summary>
        /// defines a new Dry Job that targets specified Framework
        /// </summary>
        /// <param name="targetFrameworkMoniker">Target Framework to test.</param>
        public DryJobAttribute(TargetFrameworkMoniker targetFrameworkMoniker)
            : base(GetJob(targetFrameworkMoniker, null, null))
        {
        }

        /// <summary>
        /// defines a new Dry Job that targets specified Framework, JIT and Platform
        /// </summary>
        /// <param name="targetFrameworkMoniker">Target Framework to test.</param>
        public DryJobAttribute(TargetFrameworkMoniker targetFrameworkMoniker, Jit jit, Platform platform)
            : base(GetJob(targetFrameworkMoniker, jit, platform))
        {
        }

        private static Job GetJob(TargetFrameworkMoniker targetFrameworkMoniker, Jit? jit, Platform? platform)
        {
            var baseJob = GetBaseJob(targetFrameworkMoniker.GetRuntime());
            var id = baseJob.Id;

            if (jit.HasValue)
            {
                baseJob = baseJob.With(jit.Value);
                id += "-" + jit.Value;
            }

            if (platform.HasValue)
            {
                baseJob = baseJob.With(platform.Value);
                id += "-" + platform.Value;
            }

            return baseJob.With(targetFrameworkMoniker.GetToolchain()).WithId(id);
        }

        private static Job GetBaseJob(Runtime runtime)
        {
            switch (runtime)
            {
                case CoreRtRuntime _:
                    return Job.DryCoreRT;
                case CoreRuntime _:
                    return Job.DryCore;
                case ClrRuntime _:
                    return Job.DryClr;
                case MonoRuntime _:
                    return Job.DryMono;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }
    }
}