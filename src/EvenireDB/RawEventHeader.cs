using System.Reflection.PortableExecutable;

namespace EvenireDB
{
    internal struct RawEventHeader
    {
        public Guid EventId;
        public long DataPosition;
        public byte[] EventType;
        public short EventTypeLength;
        public int EventDataLength;

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

        public readonly void ToBytes(ref byte[] buffer)
        {
            // TODO: avoid BitConverter

            // event index
            Array.Copy(this.EventId.ToByteArray(), 0, buffer, EVENT_ID_POS, TypeSizes.GUID);

            // offset in the main stream
            Array.Copy(BitConverter.GetBytes(this.DataPosition), 0, buffer, OFFSET_POS, sizeof(long));

            // event type
            Array.Copy(this.EventType, 0, buffer, EVENT_TYPE_NAME_POS, Constants.MAX_EVENT_TYPE_LENGTH);

            // event type length
            Array.Copy(BitConverter.GetBytes(this.EventTypeLength), 0, buffer, EVENT_TYPE_NAME_LENGTH_POS, sizeof(short));

            // event data length
            Array.Copy(BitConverter.GetBytes(this.EventDataLength), 0, buffer, EVENT_DATA_LENGTH_POS, sizeof(int));
        }

        public RawEventHeader(ReadOnlyMemory<byte> data) : this(data.Span) { }

        public RawEventHeader(ReadOnlySpan<byte> data)
        {
            this.EventId = new Guid(data.Slice(EVENT_ID_POS, TypeSizes.GUID));
            this.DataPosition = BitConverter.ToInt32(data.Slice(OFFSET_POS));
            this.EventType = data.Slice(EVENT_TYPE_NAME_POS, Constants.MAX_EVENT_TYPE_LENGTH).ToArray();
            this.EventTypeLength = BitConverter.ToInt16(data.Slice(EVENT_TYPE_NAME_LENGTH_POS));
            this.EventDataLength = BitConverter.ToInt32(data.Slice(EVENT_DATA_LENGTH_POS));
        }
    }
}