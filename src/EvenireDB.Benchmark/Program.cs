using BenchmarkDotNet.Running;
using EvenireDB;
using ObjectLayoutInspector;

BenchmarkRunner.Run<GetBytesBenchmark>();

//TypeLayout.PrintLayout<RawEventHeader>();

