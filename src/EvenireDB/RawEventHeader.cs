using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB
{
    //TODO: evaluate https://github.com/Cysharp/MemoryPack or https://github.com/MessagePack-CSharp/MessagePack-CSharp    
    internal readonly struct RawEventHeader
    {
        public readonly long EventIdTimestamp;
        public readonly int EventIdSequence;
        public readonly long DataPosition;
        public readonly byte[] EventType; // should always be size Constants.MAX_EVENT_TYPE_LENGTH
        public readonly short EventTypeLength;
        public readonly int EventDataLength;

        public const int SIZE =
            sizeof(long) + // event id timestamp
            sizeof(int) + // event id sequence
            sizeof(long) + // offset in the main stream
            Constants.MAX_EVENT_TYPE_LENGTH + // type name
            sizeof(short) + // type name length
            sizeof(int) // data length
        ;

        private const int EVENT_ID_TIMESTAMP_POS = 0;
        private const int EVENT_ID_SEQUENCE_POS = EVENT_ID_TIMESTAMP_POS + sizeof(long);
        private const int OFFSET_POS = EVENT_ID_SEQUENCE_POS + sizeof(int);
        private const int EVENT_TYPE_NAME_POS = OFFSET_POS + sizeof(long);
        private const int EVENT_TYPE_NAME_LENGTH_POS = EVENT_TYPE_NAME_POS + Constants.MAX_EVENT_TYPE_LENGTH;
        private const int EVENT_DATA_LENGTH_POS = EVENT_TYPE_NAME_LENGTH_POS + sizeof(short);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToBytes(ref byte[] buffer)
        {
            Unsafe.As<byte, long>(ref buffer[EVENT_ID_TIMESTAMP_POS]) = this.EventIdTimestamp;
            Unsafe.As<byte, int>(ref buffer[EVENT_ID_SEQUENCE_POS]) = this.EventIdSequence;

            // offset in the main stream            
            Unsafe.As<byte, long>(ref buffer[OFFSET_POS]) = this.DataPosition;

            // event type
            Array.Copy(this.EventType, 0, buffer, EVENT_TYPE_NAME_POS, Constants.MAX_EVENT_TYPE_LENGTH);

            // event type length
            Unsafe.As<byte, short>(ref buffer[EVENT_TYPE_NAME_LENGTH_POS]) = this.EventTypeLength;

            // event data length
            Unsafe.As<byte, int>(ref buffer[EVENT_DATA_LENGTH_POS]) = this.EventDataLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawEventHeader(ReadOnlyMemory<byte> data) : this(data.Span) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawEventHeader(ReadOnlySpan<byte> data)
        {
            this.EventIdTimestamp = BitConverter.ToInt64(data.Slice(EVENT_ID_TIMESTAMP_POS, sizeof(long)));
            this.EventIdSequence = BitConverter.ToInt32(data.Slice(EVENT_ID_SEQUENCE_POS, sizeof(int)));
            this.DataPosition = BitConverter.ToInt32(data.Slice(OFFSET_POS, sizeof(long)));
            this.EventType = data.Slice(EVENT_TYPE_NAME_POS, Constants.MAX_EVENT_TYPE_LENGTH).ToArray();
            this.EventTypeLength = BitConverter.ToInt16(data.Slice(EVENT_TYPE_NAME_LENGTH_POS, sizeof(short)));
            this.EventDataLength = BitConverter.ToInt32(data.Slice(EVENT_DATA_LENGTH_POS, sizeof(int)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawEventHeader(EventId eventId, byte[] eventType, long dataPosition, int eventDataLength, short eventTypeLength)
        {
            EventIdTimestamp = eventId.Timestamp;
            EventIdSequence = eventId.Sequence;
            EventType = eventType;
            DataPosition = dataPosition;
            EventDataLength = eventDataLength;
            EventTypeLength = eventTypeLength;
        }
    }
}