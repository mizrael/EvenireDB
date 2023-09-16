using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var config = ManualConfig.Create(DefaultConfig.Instance)
                          .WithOptions(ConfigOptions.DisableOptimizationsValidator);
BenchmarkRunner.Run<FileEventsRepositoryBenckmarks>(config);