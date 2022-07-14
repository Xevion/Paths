using BenchmarkDotNet.Running;

namespace Paths.Benchmarks {
    public static class Program {
        // `make bench` runs everything; pass --filter to pick one when iterating on a single number.
        public static void Main(string[] args) =>
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
