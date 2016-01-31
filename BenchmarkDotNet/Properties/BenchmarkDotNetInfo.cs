﻿namespace BenchmarkDotNet.Properties
{
    public static class BenchmarkDotNetInfo
    {
        public const string Title = "BenchmarkDotNet" + (IsDevelopVersion ? "-Dev" : "");
        public const string Description = "A .NET library for benchmarking";
        public const string Copyright = "Copyright © Andrey Akinshin, Jon Skeet, Matt Warren 2013–2016";
        public const string Version = "0.8.2";
        public const string FullVersion = "0.8.2" + (IsDevelopVersion ? "+" : "");

        public const bool IsDevelopVersion = true; // Set to false for NuGet publishing
    }
}