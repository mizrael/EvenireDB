namespace EvenireDB;

public readonly struct ExtentInfo
{
    public readonly required Guid StreamId { get; init; }
    public readonly required string DataPath { get; init; }
    public readonly required string HeadersPath { get; init; }
}
