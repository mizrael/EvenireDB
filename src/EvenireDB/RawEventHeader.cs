using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EvenireDB
{
    [StructLayout(LayoutKind.Sequential, Size = RawEventHeader.SIZE)]
    internal struct RawEventHeader
    {
        public Guid EventId;
        public long DataPosition;
        public short EventTypeLength;
        public int EventDataLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MAX_EVENT_TYPE_LENGTH)] 
        public byte[] EventType; // this is fixed length at Constants.MAX_EVENT_TYPE_LENGTH

        public const int SIZE =
           16 + // event id
           sizeof(long) + // offset in the main stream
           sizeof(short) + // type name length
           sizeof(int) + // data length
           Constants.MAX_EVENT_TYPE_LENGTH // type name
       ;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(ref byte[] buffer)
        {
            Unsafe.As<byte, RawEventHeader>(ref buffer[0]) = this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RawEventHeader Parse(ref byte[] data)
            => Unsafe.As<byte, RawEventHeader>(ref data[0]);
    }
}