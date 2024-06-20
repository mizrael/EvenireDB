namespace EvenireDB.Common;

public readonly record struct StreamPosition
{
    private readonly uint _value;

    public StreamPosition(uint value)
    {
        _value = value;
    }

    public static readonly StreamPosition Start = new(0);

    public static readonly StreamPosition End = new (uint.MaxValue);

    public static implicit operator uint(StreamPosition streamPosition) => streamPosition._value;

    public static implicit operator StreamPosition(uint value) => new StreamPosition(value);

    public override string ToString()
        => _value.ToString();

    public override int GetHashCode()
        => _value.GetHashCode();
}