// TODO: add creation date
public record Event
{
    public Event(string type, byte[] data, long index)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException($"'{nameof(type)}' cannot be null or whitespace.", nameof(type));
        }

        Type = type;

        if(data is null ||  data.Length == 0)
            throw new ArgumentNullException(nameof(data));
        Data = data;

        Index = index;
    }

    public string Type { get; }
    public byte[] Data { get; }
    public long Index { get; }
}
