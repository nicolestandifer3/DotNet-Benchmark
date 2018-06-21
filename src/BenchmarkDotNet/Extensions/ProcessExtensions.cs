﻿using System;
using System.Diagnostics;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Extensions
{
    // we need it public to reuse it in the auto-generated dll
    // but we hide it from intellisense with following attribute
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [PublicAPI]
    public static class ProcessExtensions
    {
        public static void EnsureHighPriority(this Process process, ILogger logger)
        {
            try
            {
                process.PriorityClass = ProcessPriorityClass.High;
            }
            catch (Exception ex)
            {
                logger.WriteLineError($"Failed to set up high priority. Make sure you have the right permissions. Message: {ex.Message}");
            }
        }
        
        internal static string ToPresentation(this IntPtr processorAffinity, int processorCount)
            => (RuntimeInformation.GetCurrentPlatform() == Platform.X64
                    ? Convert.ToString(processorAffinity.ToInt64(), 2)
                    : Convert.ToString(processorAffinity.ToInt32(), 2))
                .PadLeft(processorCount, '0');

        private static IntPtr FixAffinity(IntPtr processorAffinity)
        {
            int cpuMask = (1 << Environment.ProcessorCount) - 1;

            return RuntimeInformation.GetCurrentPlatform() == Platform.X64
                ? new IntPtr(processorAffinity.ToInt64() & cpuMask)
                : new IntPtr(processorAffinity.ToInt32() & cpuMask);
        }

        public static bool TrySetPriority(
            [NotNull] this Process process,
            ProcessPriorityClass priority,
            [NotNull] ILogger logger)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                process.PriorityClass = priority;
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLineError(
                    $"// ! Failed to set up priority {priority} for process {process}. Make sure you have the right permissions. Message: {ex.Message}");
            }

            return false;
        }

        public static bool TrySetAffinity(
            [NotNull] this Process process,
            IntPtr processorAffinity,
            [NotNull] ILogger logger)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                process.ProcessorAffinity = FixAffinity(processorAffinity);
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLineError(
                    $"// ! Failed to set up processor affinity 0x{(long)processorAffinity:X} for process {process}. Make sure you have the right permissions. Message: {ex.Message}");
            }

            return false;
        }

        public static IntPtr? TryGetAffinity([NotNull] this Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return process.ProcessorAffinity;
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }

        internal static void SetEnvironmentVariables(this ProcessStartInfo start, BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (benchmarkCase.Job.Environment.Runtime is ClrRuntime clrRuntime && !string.IsNullOrEmpty(clrRuntime.Version))
                start.EnvironmentVariables["COMPLUS_Version"] = clrRuntime.Version;

            if (!benchmarkCase.Job.HasValue(EnvironmentMode.EnvironmentVariablesCharacteristic))
                return;

            foreach (var environmentVariable in benchmarkCase.Job.Environment.EnvironmentVariables)
                start.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;
        }
    }
}