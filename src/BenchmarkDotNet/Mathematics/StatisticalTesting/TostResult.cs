using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class TostResult<T> : EquivalenceTestResult where T : OneSidedTestResult
    {
        [CanBeNull]
        public T SlowerTestResult { get; }

        [CanBeNull]
        public T FasterTestResult { get; }

        public TostResult(Threshold threshold, EquivalenceTestConclusion conclusion, [CanBeNull] T slowerTestResult, [CanBeNull] T fasterTestResult)
            : base(threshold, conclusion)
        {
            SlowerTestResult = slowerTestResult;
            FasterTestResult = fasterTestResult;
        }

        public string ToStr(bool details) => details
            ? ConclusionStr() + ": " + (SlowerTestResult?.PValueStr ?? "?") + "|" + (FasterTestResult?.PValueStr ?? "?")
            : ConclusionStr();

        private string ConclusionStr() => Conclusion == EquivalenceTestConclusion.Unknown ? "?" : Conclusion.ToString();
    }
}