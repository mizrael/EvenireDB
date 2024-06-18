namespace EvenireDB.Common;

public readonly record struct StreamType
{
   public string Value { get; }

    public StreamType(string value)
    {
        //TODO: add proper validation
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

        Value = value;
    }

    public static implicit operator string(StreamType streamType) => streamType.Value;
    public static implicit operator StreamType(string value) => new(value);
}