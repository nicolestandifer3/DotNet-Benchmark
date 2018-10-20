﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ArgumentsTests : BenchmarkTestExecutor
    {
        public static IEnumerable<object[]> GetToolchains()
            => RuntimeInformation.IsMono // https://github.com/mono/mono/issues/8397
                ? Array.Empty<object[]>()
                : new[]
                {
                    new object[] { Job.Default.GetToolchain() },
                    new object[] { InProcessEmitToolchain.Instance },
#if NETCOREAPP2_1 
// we don't want to test CoreRT twice (for .NET 4.6 and Core 2.1) when running the integration tests (these tests take a lot of time)
// we test against specific version to keep this test stable
                    new object[] { CoreRtToolchain.CreateBuilder().UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-26414-01").ToToolchain() }
    #endif
                };
        public ArgumentsTests(ITestOutputHelper output) : base(output) { }


        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsArePassedToBenchmarks(IToolchain toolchain) => CanExecute<WithArguments>(toolchain);

        public class WithArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }

            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public Task SimpleAsync(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");

                return Task.CompletedTask;
            }

            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public ValueTask<int> SimpleValueTaskAsync(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");

                return new ValueTask<int>(0);
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsFromSourceArePassedToBenchmarks(IToolchain toolchain) => CanExecute<WithArgumentsSource>(toolchain);

        public class WithArgumentsSource
        {
            [Benchmark]
            [ArgumentsSource(nameof(ArgumentsProvider))]
            public void Simple(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }

            public IEnumerable<object[]> ArgumentsProvider()
            {
                yield return new object[] { true, 1 };
                yield return new object[] { false, 2 };
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsCanBePassedByReferenceToBenchmark(IToolchain toolchain) => CanExecute<WithRefArguments>(toolchain);

        public class WithRefArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(ref bool boolean, ref int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void NonCompileTimeConstantsCanBeReturnedFromSource(IToolchain toolchain) => CanExecute<WithComplexTypesReturnedFromSources>(toolchain);

        public class WithComplexTypesReturnedFromSources
        {
            [ParamsSource(nameof(DictionaryAsParam))]
            public Dictionary<int, string> DictionaryParamInstance;

            [ParamsSource(nameof(SameButStatic))]
            public Dictionary<int, string> DictionaryParamStatic;

            [Benchmark]
            [ArgumentsSource(nameof(NonPrimitive))]
            public void Simple(SomeClass someClass, SomeStruct someStruct)
            {
                if (DictionaryParamInstance[1234] != "it's an instance getter")
                    throw new InvalidOperationException("Incorrect dictionary (instance");

                if (DictionaryParamStatic[1234] != "it's a static getter")
                    throw new InvalidOperationException("Incorrect dictionary (static)");

                if (!(someStruct.RangeEnd == 100 || someStruct.RangeEnd == 1000))
                    throw new InvalidOperationException("Incorrect struct values were passed");

                if (someStruct.RangeEnd != someClass.Values.Length)
                    throw new InvalidOperationException("Incorrect length");

                for (int i = 0; i < someStruct.RangeEnd; i++)
                    if (someClass.Values[i] != i * 2)
                        throw new InvalidOperationException("Incorrect array values were passed");
            }

            public IEnumerable<object[]> NonPrimitive()
            {
                yield return new object[] { new SomeClass(Enumerable.Range(0, 100).ToArray()), new SomeStruct(100) };
                yield return new object[] { new SomeClass(Enumerable.Range(0, 1000).ToArray()), new SomeStruct(1000) };
            }

            public IEnumerable<object> DictionaryAsParam => new object[] { new Dictionary<int, string>() { { 1234, "it's an instance getter" } } };

            public static IEnumerable<object> SameButStatic => new object[] { new Dictionary<int, string>() { { 1234, "it's a static getter" } } };

            public class SomeClass
            {
                public SomeClass(int[] initialValues) => Values = initialValues.Select(val => val * 2).ToArray();

                public int[] Values { get; }
            }

            public struct SomeStruct
            {
                public SomeStruct(int rangeEnd) => RangeEnd = rangeEnd;

                public int RangeEnd { get; }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArrayCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithArray>(toolchain);

        public class WithArray
        {
            [Benchmark]
            [Arguments(new[] { 0, 1, 2 })]
            public void AcceptingArray(int[] array)
            {
                if (array.Length != 3)
                    throw new InvalidOperationException("Incorrect array length");

                for (int i = 0; i < 3; i++)
                    if (array[i] != i)
                        throw new InvalidOperationException($"Incorrect array element at index {i}, was {array[i]} instead of {i}");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void JaggedArrayCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithJaggedArray>(toolchain);

        public class WithJaggedArray
        {
            [Benchmark]
            [ArgumentsSource(nameof(CreateMatrix))]
            public void Test(int[][] array)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                for (int i = 0; i < 10; i++)
                for (int j = 0; j < i; j++)
                    if (array[i][j] != i)
                        throw new ArgumentException("Invalid value");
            }

            public IEnumerable<object> CreateMatrix()
            {
                int[][] jagged = new int[10][];

                for (int i = 0; i < jagged.Length; i++)
                {
                    int[] row = new int[i];

                    for (int j = 0; j < i; j++)
                        row[j] = i;

                    jagged[i] = row;
                }

                yield return jagged;
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void GenericTypeCanBePassedByRefAsArgument(IToolchain toolchain) => CanExecute<WithGenericByRef>(toolchain);

        public class WithGenericByRef
        {
            public class Generic<T1, T2>
            {
                public T1 Item1;
                public T2 Item2;

                public Generic(T1 item1, T2 item2)
                {
                    Item1 = item1;
                    Item2 = item2;
                }
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetInputData))]
            public bool ValueTupleCompareNoOpt(ref Generic<int, string> byRef)
            {
                if (byRef == null)
                    throw new ArgumentNullException(nameof(byRef));

                if (byRef.Item1 != 3 || byRef.Item2 != "red")
                    throw new ArgumentException("Wrong values");

                return true;
            }

            public IEnumerable<object> GetInputData()
            {
                yield return new Generic<int, string>(3, "red");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnArrayOfTypeWithNoParameterlessCtorCanBePassedAsArgument(IToolchain toolchain) => CanExecute<WithArrayOfStringAsArgument>(toolchain);

        public class WithArrayOfStringAsArgument
        {
            [Benchmark]
            [Arguments(new object[1] { new string[0] })]
            // arguments accept "params object[]", when we pass just a string[] it's recognized as an array of params
            public void TypeReflectionArrayGetType(object anArray)
            {
                string[] strings = (string[])anArray;

                if (strings.Length != 0)
                    throw new ArgumentException("The array should be empty");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnArrayCanBePassedToBenchmarkAsSpan(IToolchain toolchain) => CanExecute<WithArrayToSpan>(toolchain);

        public class WithArrayToSpan
        {
            [Benchmark]
            [Arguments(new[] { 0, 1, 2 })]
            public void AcceptsSpan(Span<int> span)
            {
                if (span.Length != 3)
                    throw new ArgumentException("Invalid length");

                for (int i = 0; i < 3; i++)
                    if (span[i] != i)
                        throw new ArgumentException("Invalid value");
            }
        }

        [TheoryNetCore21PlusOnly("the implicit cast operator is available only in .NET Core 2.1+ (See https://github.com/dotnet/corefx/issues/30121 for more)"),
         MemberData(nameof(GetToolchains))]
        public void StringCanBePassedToBenchmarkAsReadOnlySpan(IToolchain toolchain) => CanExecute<WithStringToReadOnlySpan>(toolchain);


        public class WithStringToReadOnlySpan
        {
            private const string expectedString = "very nice string";

            [Benchmark]
            [Arguments(expectedString)]
            public void AcceptsReadOnlySpan(ReadOnlySpan<char> notString)
            {
                string aString = notString.ToString();

                if (aString != expectedString)
                    throw new ArgumentException("Invalid value");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnArrayOfStringsCanBeUsedAsArgument(IToolchain toolchain) =>
            CanExecute<WithArrayOfStringFromArgumentSource>(toolchain);

        public class WithArrayOfStringFromArgumentSource
        {
            public IEnumerable<object> GetArrayOfString()
            {
                yield return new string[123];
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetArrayOfString))]
            public void TypeReflectionArrayGetType(string[] array)
            {
                if (array.Length != 123)
                    throw new ArgumentException("The array was empty");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void BenchmarkCanAcceptFewArrays(IToolchain toolchain) => CanExecute<FewArrays>(toolchain);

        public class FewArrays
        {
            public IEnumerable<object[]> GetArrays()
            {
                yield return new object[2]
                {
                    new int[] { 0, 2, 4 },
                    new int[] { 1, 3, 5 },
                };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetArrays))]
            public void AcceptsArrays(int[] even, int[] notEven)
            {
                if (even.Length != 3 || notEven.Length != 3)
                    throw new ArgumentException("Incorrect length");

                if (!even.All(n => n % 2 == 0))
                    throw new ArgumentException("Not even");

                if (!notEven.All(n => n % 2 != 0))
                    throw new ArgumentException("Even");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void VeryBigIntegersAreSupported(IToolchain toolchain) => CanExecute<WithVeryBigInteger>(toolchain);

        public class WithVeryBigInteger
        {
            public IEnumerable<object> GetVeryBigInteger()
            {
                yield return BigInteger.Parse(new string(Enumerable.Repeat('1', 1000).ToArray()));
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetVeryBigInteger))]
            public void Method(BigInteger passed)
            {
                BigInteger expected = GetVeryBigInteger().OfType<BigInteger>().Single();

                if (expected != passed)
                    throw new ArgumentException("The BigInteger has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void SpecialDoubleValuesAreSupported(IToolchain toolchain) => CanExecute<WithSpecialDoubleValues>(toolchain);

        public class WithSpecialDoubleValues
        {
            public IEnumerable<object[]> GetSpecialDoubleValues()
            {
                yield return new object[] { double.Epsilon, nameof(double.Epsilon) };
                yield return new object[] { double.MinValue, nameof(double.MinValue) };
                yield return new object[] { double.MaxValue, nameof(double.MaxValue) };
                yield return new object[] { double.NaN, nameof(double.NaN) };
                yield return new object[] { double.NegativeInfinity, nameof(double.NegativeInfinity) };
                yield return new object[] { double.PositiveInfinity, nameof(double.PositiveInfinity) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetSpecialDoubleValues))]
            public void Method(double passed, string name)
            {
                switch (name)
                {
                    case nameof(double.Epsilon):
                        if (passed != double.Epsilon) throw new InvalidOperationException($"Unable to pass {nameof(double.Epsilon)}");
                        break;
                    case nameof(double.MaxValue):
                        if (passed != double.MaxValue) throw new InvalidOperationException($"Unable to pass {nameof(double.MaxValue)}");
                        break;
                    case nameof(double.MinValue):
                        if (passed != double.MinValue) throw new InvalidOperationException($"Unable to pass {nameof(double.MinValue)}");
                        break;
                    case nameof(double.NaN):
                        if (!double.IsNaN(passed)) throw new InvalidOperationException($"Unable to pass {nameof(double.NaN)}");
                        break;
                    case nameof(double.PositiveInfinity):
                        if (!double.IsPositiveInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(double.PositiveInfinity)}");
                        break;
                    case nameof(double.NegativeInfinity):
                        if (!double.IsNegativeInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(double.NegativeInfinity)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown case! {name}");
                }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void SpecialFloatValuesAreSupported(IToolchain toolchain) => CanExecute<WithSpecialFloatValues>(toolchain);

        public class WithSpecialFloatValues
        {
            public IEnumerable<object[]> GetSpecialFloatValues()
            {
                yield return new object[] { float.Epsilon, nameof(float.Epsilon) };
                yield return new object[] { float.MinValue, nameof(float.MinValue) };
                yield return new object[] { float.MaxValue, nameof(float.MaxValue) };
                yield return new object[] { float.NaN, nameof(float.NaN) };
                yield return new object[] { float.NegativeInfinity, nameof(float.NegativeInfinity) };
                yield return new object[] { float.PositiveInfinity, nameof(float.PositiveInfinity) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetSpecialFloatValues))]
            public void Method(float passed, string name)
            {
                switch (name)
                {
                    case nameof(float.Epsilon):
                        if (passed != float.Epsilon) throw new InvalidOperationException($"Unable to pass {nameof(float.Epsilon)}");
                        break;
                    case nameof(float.MaxValue):
                        if (passed != float.MaxValue) throw new InvalidOperationException($"Unable to pass {nameof(float.MaxValue)}");
                        break;
                    case nameof(float.MinValue):
                        if (passed != float.MinValue) throw new InvalidOperationException($"Unable to pass {nameof(float.MinValue)}");
                        break;
                    case nameof(float.NaN):
                        if (!float.IsNaN(passed)) throw new InvalidOperationException($"Unable to pass {nameof(float.NaN)}");
                        break;
                    case nameof(float.PositiveInfinity):
                        if (!float.IsPositiveInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(float.PositiveInfinity)}");
                        break;
                    case nameof(float.NegativeInfinity):
                        if (!float.IsNegativeInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(float.NegativeInfinity)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown case! {name}");
                }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void SpecialDecimalValuesAreSupported(IToolchain toolchain) => CanExecute<WithSpecialDecimalValues>(toolchain);

        public class WithSpecialDecimalValues
        {
            public IEnumerable<object[]> GetSpecialDecimalValues()
            {
                yield return new object[] { decimal.MaxValue, nameof(decimal.MaxValue) };
                yield return new object[] { decimal.MinValue, nameof(decimal.MinValue) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetSpecialDecimalValues))]
            public void Method(decimal passed, string name)
            {
                switch (name)
                {
                    case nameof(decimal.MaxValue):
                        if (passed != decimal.MaxValue) throw new InvalidOperationException($"Unable to pass {nameof(decimal.MaxValue)}");
                        break;
                    case nameof(decimal.MinValue):
                        if (passed != decimal.MinValue) throw new InvalidOperationException($"Unable to pass {nameof(decimal.MinValue)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown case! {name}");
                }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void DateTimeCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithDateTime>(toolchain);

        public class WithDateTime
        {
            public IEnumerable<object> DateTimeValues()
            {
                yield return new DateTime(2018, 8, 15);
            }

            [Benchmark]
            [ArgumentsSource(nameof(DateTimeValues))]
            public void Test(DateTime passed)
            {
                DateTime expected = DateTimeValues().OfType<DateTime>().Single();

                if (expected != passed)
                    throw new ArgumentException("The DateTime has wrong value!");
            }
        }

        [Fact]
        public void CustomTypeThatAlsoExistsInTheSystemNamespaceAsArgument() => CanExecute<WithDateTime>();

        public class CustomTypeThatAlsoExistsInTheSystemNamespace
        {
            public enum Action
            {
                It, Is, A, Duplicate, Of, System, Dot, Action
            }

            [Benchmark]
            [Arguments(Action.System)]
            public void Test(Action passed)
            {
                Action expected = Action.System;

                if (expected != passed)
                    throw new ArgumentException("The passed enum has wrong value!");
            }

        private void CanExecute<T>(IToolchain toolchain)
        {
            var config = CreateSimpleConfig(job: Job.Dry.With(toolchain));
            CanExecute<T>(config);
        }
    }
}