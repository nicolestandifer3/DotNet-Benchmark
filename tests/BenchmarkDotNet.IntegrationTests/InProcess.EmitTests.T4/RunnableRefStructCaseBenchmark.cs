﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

using System;

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    /// <summary>
    /// Generated class to check emitted msil cases
    /// </summary>
    public class RunnableRefStructCaseBenchmark
    {
        // ---- Begin StructCase(Span<int>) ----

        [Benchmark, Arguments(new[] { 0, 1, 2 }, "1", 0.1)]
        public Span<int> RefStructCase1(Span<int> x, ref string y, double? z) => x;

        // ---- Begin StructCase(ReadOnlySpan<string>) ----

        [Benchmark, Arguments(new[] { "A", "B", "C" }, "2", 0.2)]
        public ReadOnlySpan<string> RefStructCase2(ReadOnlySpan<string> x, ref string y, double? z) => x;

        // ---- Begin StructCase(Memory<int>) ----

        [Benchmark, Arguments(new[] { 0, 1, 2 }, "3", 0.3)]
        public Memory<int> RefStructCase3(Memory<int> x, ref string y, double? z) => x;

        // ---- Begin StructCase(ReadOnlyMemory<string>) ----

        [Benchmark, Arguments(new[] { "A", "B", "C" }, "4", 0.4)]
        public ReadOnlyMemory<string> RefStructCase4(ReadOnlyMemory<string> x, ref string y, double? z) => x;

    }
}