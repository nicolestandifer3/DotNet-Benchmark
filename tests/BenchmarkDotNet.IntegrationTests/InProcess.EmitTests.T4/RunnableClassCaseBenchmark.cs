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
using System.Threading.Tasks;

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    /// <summary>
    /// Generated class to check emitted msil cases
    /// </summary>
    public class RunnableClassCaseBenchmark
    {
        // ---- Begin ClassCase(string) ----

        private static string _staticRefResultHolder1;
        private string _refResultHolder1;

        [Benchmark]
        public string ClassCase1() => default;

        [Benchmark, Arguments(null, "1", 0.1)]
        public string ClassCase1(string x, ref string y, double? z) => default;

        [Benchmark, Arguments(null, "1", 0.1)]
        public static string StaticClassCase1(string x, ref string y, double? z) => default;

        [Benchmark]
        public ref string RefReturnClassCase1() => ref _refResultHolder1;

        [Benchmark, Arguments(null, "1", 0.1)]
        public ref string RefReturnClassCase1(string x, ref string y, double? z) => ref _refResultHolder1;

        [Benchmark, Arguments(null, "1", 0.1)]
        public static ref string StaticRefReturnClassCase1(string x, ref string y, double? z) => ref _staticRefResultHolder1;

        // ---- Begin ClassCase(CustomClassConsumable<int>) ----

        private static CustomClassConsumable<int> _staticRefResultHolder2;
        private CustomClassConsumable<int> _refResultHolder2;

        [Benchmark]
        public CustomClassConsumable<int> ClassCase2() => default;

        [Benchmark, Arguments(null, "2", 0.2)]
        public CustomClassConsumable<int> ClassCase2(CustomClassConsumable<int> x, ref string y, double? z) => default;

        [Benchmark, Arguments(null, "2", 0.2)]
        public static CustomClassConsumable<int> StaticClassCase2(CustomClassConsumable<int> x, ref string y, double? z) => default;

        [Benchmark]
        public ref CustomClassConsumable<int> RefReturnClassCase2() => ref _refResultHolder2;

        [Benchmark, Arguments(null, "2", 0.2)]
        public ref CustomClassConsumable<int> RefReturnClassCase2(CustomClassConsumable<int> x, ref string y, double? z) => ref _refResultHolder2;

        [Benchmark, Arguments(null, "2", 0.2)]
        public static ref CustomClassConsumable<int> StaticRefReturnClassCase2(CustomClassConsumable<int> x, ref string y, double? z) => ref _staticRefResultHolder2;

        // ---- Begin ClassCase(CustomClassConsumable<string>) ----

        private static CustomClassConsumable<string> _staticRefResultHolder3;
        private CustomClassConsumable<string> _refResultHolder3;

        [Benchmark]
        public CustomClassConsumable<string> ClassCase3() => default;

        [Benchmark, Arguments(null, "3", 0.3)]
        public CustomClassConsumable<string> ClassCase3(CustomClassConsumable<string> x, ref string y, double? z) => default;

        [Benchmark, Arguments(null, "3", 0.3)]
        public static CustomClassConsumable<string> StaticClassCase3(CustomClassConsumable<string> x, ref string y, double? z) => default;

        [Benchmark]
        public ref CustomClassConsumable<string> RefReturnClassCase3() => ref _refResultHolder3;

        [Benchmark, Arguments(null, "3", 0.3)]
        public ref CustomClassConsumable<string> RefReturnClassCase3(CustomClassConsumable<string> x, ref string y, double? z) => ref _refResultHolder3;

        [Benchmark, Arguments(null, "3", 0.3)]
        public static ref CustomClassConsumable<string> StaticRefReturnClassCase3(CustomClassConsumable<string> x, ref string y, double? z) => ref _staticRefResultHolder3;

    }
}