using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;

namespace EvenireDB.Benchmark;

public class Program
{
    public static int Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddExporter(JsonExporter.Brief)
            .AddExporter(HtmlExporter.Default)
            .AddLogger(ConsoleLogger.Default);

        // Add CI-specific configuration
        if (args.Contains("--ci"))
        {
            config = config
                .WithOptions(ConfigOptions.DontOverwriteResults)
                .WithArtifactsPath("./BenchmarkDotNet.Artifacts");
        }

        var summary = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, config);

        return summary.Any(s => s.HasCriticalValidationErrors) ? 1 : 0;
    }
}