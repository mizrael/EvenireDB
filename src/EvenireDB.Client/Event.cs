using EvenireDB.Common;
using System.Text.Json;

namespace EvenireDB.Client
{
    public record EventId(ulong Timestamp, ushort Sequence);

    public record Event
    {
        public Event(EventId id, string type, ReadOnlyMemory<byte> data)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException($"'{nameof(type)}' cannot be null or whitespace.", nameof(type));

            if (type.Length > Constants.MAX_EVENT_TYPE_LENGTH)
                throw new ArgumentOutOfRangeException($"event type cannot be longer than {Constants.MAX_EVENT_TYPE_LENGTH} characters.", nameof(type));

            Id = id;
            Type = type;

            if (data.Length == 0)
                throw new ArgumentNullException(nameof(data));
            Data = data;
        }

        public EventId Id { get; }
        public string Type { get; }

        public ReadOnlyMemory<byte> Data { get; }

        public static Event Create<T>(T payload, string type = "")
        {
            if (string.IsNullOrWhiteSpace(type))
                type = typeof(T).Name;
            var bytes = JsonSerializer.SerializeToUtf8Bytes<T>(payload);
            return new Event(Guid.NewGuid(), type, bytes);
        }
    }
}