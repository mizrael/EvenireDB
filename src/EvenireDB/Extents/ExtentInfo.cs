namespace EvenireDB.Extents;

public readonly struct ExtentInfo
{
    public readonly required string DataPath { get; init; }
    public readonly required string HeadersPath { get; init; }
}
