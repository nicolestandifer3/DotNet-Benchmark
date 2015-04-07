﻿using System.Linq;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Samples;

namespace Benchmarks
{
    public class AttributesSample : ISample
    {
        public void Run()
        {
            new BenchmarkRunner().RunCompetition(new AttributesSampleCompetition());
        }

        public class AttributesSampleCompetition
        {
            private const int IterationCount = 10;
            private const int ArraySize = 1024 * 1024;

            private int[] array;

            [BenchmarkMethodInitialize]
            public void ForInitialize()
            {
                array = new int[ArraySize];
            }

            [BenchmarkMethod]
            public int For()
            {
                int sum = 0;
                for (int iteration = 0; iteration < IterationCount; iteration++)
                    for (int i = 0; i < array.Length; i++)
                        sum += array[i];
                return sum;
            }

            [BenchmarkMethodClean]
            public void ForClean()
            {
                array = null;
            }

            [BenchmarkMethodInitialize]
            public void LinqInitialize()
            {
                array = new int[ArraySize];
            }

            [BenchmarkMethod]
            public int Linq()
            {
                int sum = 0;
                for (int iteration = 0; iteration < IterationCount; iteration++)
                    sum += array.Sum();
                return sum;
            }

            [BenchmarkMethodClean]
            public void LinqClean()
            {
                array = null;
            }
        }
    }

}