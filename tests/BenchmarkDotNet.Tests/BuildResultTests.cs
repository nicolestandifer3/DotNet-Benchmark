﻿using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using System;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class BuildResultTests
    {
        [Fact]
        public void NotFullyCompatibleMsBuildErrorIsTranslatedToMoreUserFriendlyVersion()
        {
            const string msbuildError = @"C:\Program Files\dotnet\sdk\3.0.100-preview9-013617\Microsoft.Common.CurrentVersion.targets(1653,5): warning NU1702: ProjectReference 'C:\Projects\BenchmarkDotNet\tests\BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetFramework\BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetFramework.csproj' was resolved using '.NETFramework,Version=v4.6.2' instead of the project target framework '.NETCoreApp,Version=v2.1'. This project may not be fully compatible with your project. [C:\Projects\BenchmarkDotNet\tests\BenchmarkDotNet.IntegrationTests\bin\Release\net462\Job-VUALUD\BenchmarkDotNet.Autogenerated.csproj]";

            string expected = $@"The project which defines benchmarks does not target 'netcoreapp2.1'." + Environment.NewLine +
                $"You need to add 'netcoreapp2.1' to <TargetFrameworks> in your project file " +
                @"('C:\Projects\BenchmarkDotNet\tests\BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetFramework\BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetFramework.csproj')." + Environment.NewLine +
                "Example: <TargetFrameworks>net462;netcoreapp2.1</TargetFrameworks>";
            Verify(msbuildError, true, expected);
        }

        [Fact]
        public void NotCompatibleMsBuildErrorIsTranslatedToMoreUserFriendlyVersion()
        {
            const string msbuildError = @"error NU1201: Project BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetCore is not compatible with net462 (.NETFramework,Version=v4.6.2) / win7-x64. Project BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetCore supports: netcoreapp2.1 (.NETCoreApp,Version=v2.1)";

            string expected = $@"The project which defines benchmarks does not target 'net462'." + Environment.NewLine +
                $"You need to add 'net462' to <TargetFrameworks> in your project file " +
                @"('BenchmarkDotNet.IntegrationTests.SingleRuntime.DotNetCore')." + Environment.NewLine +
                "Example: <TargetFrameworks>netcoreapp2.1;net462</TargetFrameworks>";

            Verify(msbuildError, true, expected);
        }

        [Fact]
        public void MissingSdkIsTranslatedToMoreUserFriendlyVersion()
        {
            const string msbuildError = @"C:\Program Files\dotnet\sdk\3.0.100-preview9-013617\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.TargetFrameworkInference.targets(134,5): error NETSDK1045: The current .NET SDK does not support targeting .NET Core 5.0.  Either target .NET Core 3.0 or lower, or use a version of the .NET SDK that supports .NET Core 5.0. [C:\Projects\BenchmarkDotNet\samples\BenchmarkDotNet.Samples\bin\Release\netcoreapp2.1\bad57fca-694a-41ad-b630-9e5317a782ab\BenchmarkDotNet.Autogenerated.csproj]";

            string expected = "The current .NET SDK does not support targeting .NET Core 5.0. You need to install it or pass the path to dotnet cli via the `--cli` console line argument.";

            Verify(msbuildError, true, expected);
        }

        [Fact]
        public void UnknownMSBuildErrorsAreNotTranslatedToMoreUserFriendlyVersions()
        {
            const string msbuildError = "warning NU1702: something is broken";

            Verify(msbuildError, false, null);
        }

        [AssertionMethod]
        private void Verify(string msbuildError, bool expectedResult, string expectedReason)
        {
            var sut = BuildResult.Failure(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()), msbuildError);

            Assert.Equal(expectedResult, sut.TryToExplainFailureReason(out string reason));
            Assert.Equal(expectedReason, reason);
        }
    }
}
