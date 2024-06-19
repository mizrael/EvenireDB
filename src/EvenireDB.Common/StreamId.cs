namespace EvenireDB.Common;

public readonly record struct StreamId
{
    public required readonly Guid Key { get; init; }
    public required readonly StreamType Type { get; init; }
}
