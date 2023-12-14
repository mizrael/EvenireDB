using EvenireDB.Common;

namespace EvenireDB.Client
{
    public record EventData
    {
        public EventData(string type, ReadOnlyMemory<byte> data)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException($"'{nameof(type)}' cannot be null or whitespace.", nameof(type));

            if (type.Length > Constants.MAX_EVENT_TYPE_LENGTH)
                throw new ArgumentOutOfRangeException($"event type cannot be longer than {Constants.MAX_EVENT_TYPE_LENGTH} characters.", nameof(type));

            Type = type;

            if (data.Length == 0)
                throw new ArgumentNullException(nameof(data));
            Data = data;
        }

        public string Type { get; }

        public ReadOnlyMemory<byte> Data { get; }
    }
}