using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Trait("Category", "JobTests")]
    public static class JobTests
    {
        private static void AssertProperties(CharacteristicObject obj, string properties) =>
            Assert.Equal(CharacteristicObject.IdCharacteristic.ResolveValueCore(obj, null), properties);

        [Fact]
        public static void Test01Create()
        {
            var j = new Job("CustomId");
            Assert.False(j.Frozen);
            Assert.False(j.Environment.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("CustomId", j.Id);
            Assert.Equal("CustomId", j.DisplayInfo);
            Assert.Equal("CustomId", j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("CustomId", j.Environment.Id);

            // freeze
            var old = j;
            j = j.Freeze();
            Assert.Same(old, j);
            j = j.Freeze();
            Assert.Same(old, j);
            Assert.True(j.Frozen);
            Assert.True(j.Environment.Frozen);
            Assert.True(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("CustomId", j.Id);
            Assert.Equal("CustomId", j.DisplayInfo);
            Assert.Equal("CustomId", j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("CustomId", j.Environment.Id);

            // unfreeze
            old = j;
            j = j.UnfreezeCopy();
            Assert.NotSame(old, j);
            Assert.False(j.Frozen);
            Assert.False(j.Environment.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("Default", j.Id); // id reset
            Assert.True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            Assert.True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("Default", j.Environment.Id);

            // new job
            j = new Job(j.Freeze());
            Assert.False(j.Frozen);
            Assert.False(j.Environment.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("Default", j.Id); // id reset
            Assert.True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            Assert.True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("Default", j.Environment.Id);
        }

        [Fact]
        public static void Test02Modify()
        {
            var j = new Job("SomeId");

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(0, j.Run.LaunchCount);

            Assert.False(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.False(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.False(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.False(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Default");
            AssertProperties(j.Environment, "Default");

            // 1. change values
            j.Environment.Platform = Platform.X64;
            j.Run.LaunchCount = 1;

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X64, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=1");
            AssertProperties(j.Environment, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2. reset Env mode (hack via Characteristic setting)
            var oldEnv = j.Environment;
            Job.EnvironmentCharacteristic[j] = new EnvironmentMode();

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.False(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.False(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "LaunchCount=1");
            AssertProperties(j.Environment, "Default");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2.1 proof that oldEnv was the same
            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X64, oldEnv.Platform);
            Assert.True(oldEnv.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.Equal("Platform=X64", oldEnv.Id);

            // 3. update Env mode (hack via Characteristic setting)
            Job.EnvironmentCharacteristic[j] = new EnvironmentMode()
            {
                Platform = Platform.X86
            };

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X86, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Environment, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 4. Freeze-unfreeze:
            j = j.Freeze().UnfreezeCopy();

            Assert.Equal("Platform=X86, LaunchCount=1", j.Id);
            Assert.Equal(Platform.X86, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Environment, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 5. Test .With extensions
            j = j.Freeze()
                .WithId("NewId");
            Assert.Equal("NewId", j.Id); // id set

            j = j.Freeze()
                .With(Platform.X64)
                .WithLaunchCount(2);

            Assert.Equal("NewId", j.Id); // id not lost
            Assert.Equal("NewId(Platform=X64, LaunchCount=2)", j.DisplayInfo);
            Assert.Equal(Platform.X64, j.Environment.Platform);
            Assert.Equal(2, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=2");
            AssertProperties(j.Environment, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=2");
        }

        [Fact]
        public static void Test03IdDoesNotFlow()
        {
            var j = new Job(EnvironmentMode.LegacyJitX64, RunMode.Long); // id will not flow, new Job
            Assert.False(j.HasValue(CharacteristicObject.IdCharacteristic));
            Assert.False(j.Environment.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvironmentCharacteristic[j] = EnvironmentMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.False(j.HasValue(CharacteristicObject.IdCharacteristic));
            Assert.False(j.Environment.HasValue(CharacteristicObject.IdCharacteristic));

            var c = new CharacteristicSet(EnvironmentMode.LegacyJitX64, RunMode.Long); // id will not flow, new CharacteristicSet
            Assert.False(c.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvironmentCharacteristic[c] = EnvironmentMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.False(c.HasValue(CharacteristicObject.IdCharacteristic));

            CharacteristicObject.IdCharacteristic[c] = "MyId"; // id set explicitly
            Assert.Equal("MyId", c.Id);

            j = new Job("MyId", EnvironmentMode.LegacyJitX64, RunMode.Long); // id set explicitly
            Assert.Equal("MyId", j.Id);
            Assert.Equal("MyId", j.Environment.Id);

            Job.EnvironmentCharacteristic[j] = EnvironmentMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.Equal("MyId", j.Id);
            Assert.Equal("MyId", j.Environment.Id);

            j = j.With(Jit.RyuJit);  // custom id will flow
            Assert.Equal("MyId", j.Id);
        }

        [Fact]
        public static void CustomJobIdIsPreserved()
        {
            const string id = "theId";

            var jobWithId = Job.Default.WithId(id);

            Assert.Equal(id, jobWithId.Id);

            var shouldHaveSameId = jobWithId.AsBaseline();

            Assert.Equal(id, shouldHaveSameId.Id);
        }

        [Fact]
        public static void PredefinedJobIdIsNotPreserved()
        {
            var predefinedJob = Job.Default;

            var customJob = predefinedJob.AsBaseline();

            Assert.NotEqual(predefinedJob.Id, customJob.Id);
        }

        [Fact]
        public static void Test04Apply()
        {
            var j = new Job()
            {
                Run = { TargetCount = 1 }
            };

            AssertProperties(j, "TargetCount=1");

            j.Apply(
                new Job
                {
                    Environment = { Platform = Platform.X64 },
                    Run = { TargetCount = 2 }
                });
            AssertProperties(j, "Platform=X64, TargetCount=2");

            // filter by properties
            j.Environment.Apply(
                new Job()
                    .With(Jit.RyuJit)
                    .WithGcAllowVeryLargeObjects(true)
                    .WithTargetCount(3)
                    .WithLaunchCount(22));
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=True, TargetCount=2");

            // apply subnode
            j.Apply(
                new GcMode()
                {
                    AllowVeryLargeObjects = false
                });
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=False, TargetCount=2");

            // Apply empty
            j.Apply(Job.Default); // does nothing
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=False, TargetCount=2");
        }

        [Fact]
        public static void Test05ApplyCharacteristicSet()
        {
            var set1 = new CharacteristicSet();
            var set2 = new CharacteristicSet();

            set1
                .Apply(
                    new EnvironmentMode
                    {
                        Platform = Platform.X64
                    })
                .Apply(
                    new Job
                    {
                        Run =
                        {
                            LaunchCount = 2
                        },
                        Environment =
                        {
                            Platform = Platform.X86
                        }
                    });
            AssertProperties(set1, "LaunchCount=2, Platform=X86");
            Assert.Equal(Platform.X86, Job.EnvironmentCharacteristic[set1].Platform);
            Assert.True(set1.HasValue(Job.EnvironmentCharacteristic));
            Assert.Equal(Platform.X86, EnvironmentMode.PlatformCharacteristic[set1]);

            set2.Apply(EnvironmentMode.RyuJitX64).Apply(new GcMode { Concurrent = true });
            Assert.Null(Job.RunCharacteristic[set2]);
            Assert.False(set2.HasValue(Job.RunCharacteristic));
            AssertProperties(set2, "Concurrent=True, Jit=RyuJit, Platform=X64");

            var temp = set1.UnfreezeCopy();
            set1.Apply(set2);
            set2.Apply(temp);
            AssertProperties(set1, "Concurrent=True, Jit=RyuJit, LaunchCount=2, Platform=X64");
            AssertProperties(set2, "Concurrent=True, Jit=RyuJit, LaunchCount=2, Platform=X86");

            var j = new Job();
            AssertProperties(j, "Default");

            j.Environment.Gc.Apply(set1);
            AssertProperties(j, "Concurrent=True");

            j.Run.Apply(set1);
            AssertProperties(j, "Concurrent=True, LaunchCount=2");

            j.Environment.Apply(set1);
            AssertProperties(j, "Jit=RyuJit, Platform=X64, Concurrent=True, LaunchCount=2");

            j.Apply(set1);
            AssertProperties(j, "Jit=RyuJit, Platform=X64, Concurrent=True, LaunchCount=2");
        }

        [Fact]
        public static void Test06CharacteristicHacks()
        {
            var j = new Job();
            Assert.Equal(0, j.Run.TargetCount);

            RunMode.TargetCountCharacteristic[j] = 123;
            Assert.Equal(123, j.Run.TargetCount);

            var old = j.Run;
            Job.RunCharacteristic[j] = new RunMode();
            Assert.Equal(0, j.Run.TargetCount);

            Job.RunCharacteristic[j] = old;
            old.TargetCount = 234;
            Assert.Equal(234, j.Run.TargetCount);
            Assert.Equal(234, RunMode.TargetCountCharacteristic[j]);

            Characteristic a = Job.RunCharacteristic;
            // will not throw:
            a[j] = new RunMode();
            Assert.Throws<ArgumentNullException>(() => a[j] = null); // nulls for job nodes are not allowed;
            Assert.Throws<ArgumentNullException>(() => a[j] = Characteristic.EmptyValue);
            Assert.Throws<ArgumentException>(() => a[j] = new EnvironmentMode()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = 123); // not assignable;

            a = InfrastructureMode.ToolchainCharacteristic;
            // will not throw:
            a[j] = CsProjClassicNetToolchain.Net46;
            a[j] = null;
            a[j] = Characteristic.EmptyValue;
            Assert.Throws<ArgumentException>(() => a[j] = new EnvironmentMode()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = 123); // not assignable;
        }

        [Fact]
        public static void Test07GetCharacteristics()
        {
            // Update expected values only if Job properties were changed.
            // Otherwise, there's a bug.
            var a = CharacteristicHelper
                .GetThisTypeCharacteristics(typeof(Job))
                .Select(c => c.Id);
            Assert.Equal("Id;Accuracy;Environment;Infrastructure;Meta;Run", string.Join(";", a));
            a = CharacteristicHelper
                .GetAllCharacteristics(typeof(Job))
                .Select(c => c.Id);
            Assert.Equal(string.Join(";", a), "Id;Accuracy;AnalyzeLaunchVariance;EvaluateOverhead;" +
                "MaxAbsoluteError;MaxRelativeError;MinInvokeCount;MinIterationTime;OutlierMode;Environment;Affinity;" +
                "Jit;Platform;Runtime;Gc;AllowVeryLargeObjects;Concurrent;CpuGroups;Force;HeapAffinitizeMask;HeapCount;NoAffinitize;" +
                "RetainVm;Server;Infrastructure;Arguments;BuildConfiguration;Clock;EngineFactory;EnvironmentVariables;Toolchain;Meta;IsBaseline;IsMutator;Run;InvocationCount;IterationTime;" +
                "LaunchCount;MaxTargetIterationCount;MinTargetIterationCount;RunStrategy;TargetCount;UnrollFactor;WarmupCount");
        }
        
        [Fact]
        public static void MutatorAppliedToOtherJobOverwritesOnlyTheConfiguredSettings()
        {
            var jobBefore = Job.Core; // this is a default job with Runtime set to Core
            var copy = jobBefore.UnfreezeCopy();
            
            Assert.False(copy.HasValue(RunMode.MaxTargetIterationCountCharacteristic));

            var mutator = Job.Default.WithMaxTargetIterationCount(20);

            copy.Apply(mutator);
            
            Assert.True(copy.HasValue(RunMode.MaxTargetIterationCountCharacteristic));
            Assert.Equal(20, copy.Run.MaxTargetIterationCount);
            Assert.False(jobBefore.HasValue(RunMode.MaxTargetIterationCountCharacteristic));
            Assert.True(copy.Environment.Runtime is CoreRuntime);
        }
    }
}