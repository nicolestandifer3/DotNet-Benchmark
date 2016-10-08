﻿using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : Attribute
    {
        public object[] Values { get; private set; }

        // CLS-Compliant Code requires a constuctor without an array in the argument list
        public ParamsAttribute()
        {
            Values = new object[0];
        }

        public ParamsAttribute(params object[] values)
        {
            Values = values;
        }
    }
}
