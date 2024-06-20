using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EvenireDB.Common;

[JsonConverter(typeof(StreamTypeJsonConverter))]
public readonly record struct StreamType : 
    IParsable<StreamType>,
    IEquatable<string>
{
    private readonly string _value;

    public StreamType(string value)
    {
        //TODO: add proper validation
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        _value = value;
    }

    public override string ToString()
       => _value;

    public override int GetHashCode()
        => _value.GetHashCode();

    public static implicit operator string(StreamType streamType) => streamType._value;
    public static implicit operator StreamType(string value) => new StreamType(value);

    public static readonly StreamType Empty = new StreamType("[empty]"); 

    public static StreamType Parse(string s, IFormatProvider? provider)
    => (StreamType)s;

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out StreamType result)
    {
        result = Empty;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            result = (StreamType)s;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(string? other)
    {
        return _value.Equals(other);
    }
}
