using BenchmarkDotNet.Running;

namespace DerivaSharp.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
