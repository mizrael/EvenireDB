using EvenireDB.Common;

namespace EvenireDB
{
    public class EventValidator : IEventValidator
    {
        private readonly uint _maxEventDataSize;

        public EventValidator(uint maxEventDataSize)
        {
            _maxEventDataSize = maxEventDataSize;
        }

        public void Validate(string type, ReadOnlyMemory<byte> data)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException($"'{nameof(type)}' cannot be null or whitespace.", nameof(type));

            if (type.Length > Constants.MAX_EVENT_TYPE_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(type), $"event type cannot be longer than {Constants.MAX_EVENT_TYPE_LENGTH} characters.");

            if (data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            if (data.Length > _maxEventDataSize)
                throw new ArgumentOutOfRangeException(nameof(data), $"event data cannot be longer than {_maxEventDataSize} bytes.");
        }
    }
}