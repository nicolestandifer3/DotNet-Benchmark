using System;

namespace BenchmarkDotNet.Build.Options;

public class BoolOption : Option<bool>
{
    public BoolOption(string commandLineName) : base(commandLineName)
    {
    }

    public override bool Resolve(BuildContext context)
    {
        if (!HasArgument(context))
            return false;
        var value = GetArgument(context);
        if (value == null)
            return true;
        return !value.Equals(false.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}