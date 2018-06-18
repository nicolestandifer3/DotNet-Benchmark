﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class CompilationValidator : IValidator
    {
        private const char Underscore = '_';

        public static readonly IValidator Default = new CompilationValidator();

        private CompilationValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => ValidateCSharpNaming(validationParameters.Benchmarks)
                    .Union(ValidateNamingConflicts(validationParameters.Benchmarks))
                    .Union(ValidateAccessModifiers(validationParameters.Benchmarks));

        private IEnumerable<ValidationError> ValidateCSharpNaming(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks
                .Where(benchmark => !IsValidCSharpIdentifier(benchmark.Target.Method.Name))
                .Distinct(BenchmarkMethodEqualityComparer.Instance) // we might have multiple jobs targeting same method. Single error should be enough ;)
                .Select(benchmark
                    => new ValidationError(
                        true,
                        $"Benchmarked method `{benchmark.Target.Method.Name}` contains illegal character(s). Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name.",
                        benchmark
                    ));

        private IEnumerable<ValidationError> ValidateNamingConflicts(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks
                .Select(benchmark => benchmark.Target.Type)
                .Distinct()
                .Where(type => type.GetAllMethods().Any(method => IsUsingNameUsedInternallyByOurTemplate(method.Name)))
                .Select(benchmark
                    => new ValidationError(
                        true,
                        "Using \"__Idle\" for method name is prohibited. We are using it internally in our templates. Please rename your method"));

        private IEnumerable<ValidationError> ValidateAccessModifiers(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks.Where(x => x.Target.Type.IsGenericType
                                     && HasPrivateGenericArguments(x.Target.Type))
                         .Select(benchmark => new ValidationError(true, $"Generic class {benchmark.Target.Type.GetDisplayName()} has non public generic argument(s)"));
        
        private bool IsValidCSharpIdentifier(string identifier) // F# allows to use whitespaces as names #479
            => !string.IsNullOrEmpty(identifier)
               && (char.IsLetter(identifier[0]) || identifier[0] == Underscore) // An identifier must start with a letter or an underscore
               && identifier
                    .Skip(1)
                    .All(character => char.IsLetterOrDigit(character) || character == Underscore);

        private bool IsUsingNameUsedInternallyByOurTemplate(string identifier)
            => identifier == "__Idle";

        private bool HasPrivateGenericArguments(Type type) => type.GetGenericArguments().Any(a => !(a.IsPublic
                                                                                                 || a.IsNestedPublic));
        
        private class BenchmarkMethodEqualityComparer : IEqualityComparer<BenchmarkCase>
        {
            internal static readonly IEqualityComparer<BenchmarkCase> Instance = new BenchmarkMethodEqualityComparer();

            public bool Equals(BenchmarkCase x, BenchmarkCase y)
                => x.Target.Method.Equals(y.Target.Method);

            public int GetHashCode(BenchmarkCase obj)
                => obj.Target.Method.GetHashCode();
        }
    }
}