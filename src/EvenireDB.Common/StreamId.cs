using System.Diagnostics.CodeAnalysis;

namespace EvenireDB.Common;

public readonly record struct StreamId
{
    public StreamId() { }

    [SetsRequiredMembers]
    public StreamId(Guid key, StreamType type)
    {
        Key = key;
        Type = type;
    }

    public required readonly Guid Key { get; init; }
    public required readonly StreamType Type { get; init; }

    public override int GetHashCode()
        => HashCode.Combine(Key, Type);

    public override string ToString()
        => $"{Type}/{Key}";
}
