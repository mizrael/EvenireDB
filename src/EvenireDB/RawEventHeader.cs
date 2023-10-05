using EvenireDB.Common;
using System.Runtime.CompilerServices;

namespace EvenireDB
{
    //TODO: evaluate https://github.com/MessagePack-CSharp/MessagePack-CSharp    
    internal readonly struct RawEventHeader
    {
        public readonly Guid EventId;
        public readonly long DataPosition;
        public readonly byte[] EventType; // should always be size Constants.MAX_EVENT_TYPE_LENGTH
        public readonly short EventTypeLength;
        public readonly int EventDataLength;

        public const int SIZE =
            TypeSizes.GUID + // index
            sizeof(long) + // offset in the main stream
            Constants.MAX_EVENT_TYPE_LENGTH + // type name
            sizeof(short) + // type name length
            sizeof(int) // data length
        ;

        private const int EVENT_ID_POS = 0;
        private const int OFFSET_POS = EVENT_ID_POS + TypeSizes.GUID;
        private const int EVENT_TYPE_NAME_POS = OFFSET_POS + sizeof(long);
        private const int EVENT_TYPE_NAME_LENGTH_POS = EVENT_TYPE_NAME_POS + Constants.MAX_EVENT_TYPE_LENGTH;
        private const int EVENT_DATA_LENGTH_POS = EVENT_TYPE_NAME_LENGTH_POS + sizeof(short);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToBytes(ref byte[] buffer)
        {
            // event index
            Array.Copy(this.EventId.ToByteArray(), 0, buffer, EVENT_ID_POS, TypeSizes.GUID);

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
            this.EventId = new Guid(data.Slice(EVENT_ID_POS, TypeSizes.GUID));
            this.DataPosition = BitConverter.ToInt32(data.Slice(OFFSET_POS));
            this.EventType = data.Slice(EVENT_TYPE_NAME_POS, Constants.MAX_EVENT_TYPE_LENGTH).ToArray();
            this.EventTypeLength = BitConverter.ToInt16(data.Slice(EVENT_TYPE_NAME_LENGTH_POS));
            this.EventDataLength = BitConverter.ToInt32(data.Slice(EVENT_DATA_LENGTH_POS));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawEventHeader(Guid eventId, byte[] eventType, long dataPosition, int eventDataLength, short eventTypeLength)
        {
            EventId = eventId;
            EventType = eventType;
            DataPosition = dataPosition;
            EventDataLength = eventDataLength;
            EventTypeLength = eventTypeLength;
        }
    }
}