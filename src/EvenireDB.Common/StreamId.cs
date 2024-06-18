namespace EvenireDB.Common;

public readonly record struct StreamId
{
    public StreamId(Guid key, string type)
    {
        Key = key;
        Type = type;
    }

    public Guid Key { get; }
    public StreamType Type { get; }
}
