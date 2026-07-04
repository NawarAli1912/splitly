using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Splitly.Benchmarks.SettlementBenchmarks).Assembly)
    .Run(args);
