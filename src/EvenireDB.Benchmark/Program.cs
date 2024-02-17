using BenchmarkDotNet.Running;

// BenchmarkRunner.Run<FileEventsRepositoryWriteBenckmarks>();

//TypeLayout.PrintLayout<RawEventHeader>();

using var proc = System.Diagnostics.Process.GetCurrentProcess();

Console.WriteLine(proc.PrivateMemorySize64);
Console.WriteLine(proc.VirtualMemorySize64);

var array = new byte[1_000_000_000];
for(int i = 0; i < array.Length; i++)
{
    array[i] = 1;
}

Console.WriteLine(proc.PrivateMemorySize64);
Console.WriteLine(proc.VirtualMemorySize64);
