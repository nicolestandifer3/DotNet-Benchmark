﻿using System.Collections;
using System.Globalization;
using System.Resources;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).Assembly()).Run(args);            
        }
    }
}
