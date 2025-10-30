using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Reflection;

// Check if running in CI mode
var isCi = args.Contains("--ci") || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

IConfig config = isCi
    ? DefaultConfig.Instance
        .WithOptions(ConfigOptions.DisableOptimizationsValidator)
        .WithOptions(ConfigOptions.DisableLogFile)
        .WithOptions(ConfigOptions.DontOverwriteResults)
    : DefaultConfig.Instance;

var summary = BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), config);

// Return non-zero exit code if any benchmarks failed
return summary.Any(s => s.HasCriticalValidationErrors) ? 1 : 0;