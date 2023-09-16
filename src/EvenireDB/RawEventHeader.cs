using System.Buffers;

internal struct RawEventHeader
{
    public int Version;
    public long EventIndex;
    public int EventTypeNameLength; 
    public int EventDataLength;    

    public const int HEADER_SIZE =
        sizeof(int) + // version
        sizeof(long) + // index
        sizeof(int) + // type name length
        sizeof(int) // data length
    ;

    private const int VERSION_POS = 0;
    private const int EVENTINDEX_POS = VERSION_POS + sizeof(int);
    private const int EVENTTYPENAME_POS = EVENTINDEX_POS + sizeof(long);
    private const int EVENTDATALENGTH_POS = EVENTTYPENAME_POS + sizeof(int);

    public readonly void Fill(byte[] buffer)
    {
        // TODO: benchmark other methods
        // TODO: avoid BitConverter

        // version
        Array.Copy(VersionBytes.V1, 0, buffer, VERSION_POS, VersionBytes.V1.Length);

        // event index
        Array.Copy(BitConverter.GetBytes(this.EventIndex), 0, buffer, EVENTINDEX_POS, sizeof(long));

        // event type length
        Array.Copy(BitConverter.GetBytes(this.EventTypeNameLength), 0, buffer, EVENTTYPENAME_POS, sizeof(int));

        // event data length
        Array.Copy(BitConverter.GetBytes(this.EventDataLength), 0, buffer, EVENTDATALENGTH_POS, sizeof(int));
    }

    public static void Parse(byte[] data, ref RawEventHeader header)
    {
        header.Version = BitConverter.ToInt32(data, VERSION_POS);
        header.EventIndex = BitConverter.ToInt64(data, EVENTINDEX_POS);
        header.EventTypeNameLength = BitConverter.ToInt32(data, EVENTTYPENAME_POS);
        header.EventDataLength = BitConverter.ToInt32(data, EVENTDATALENGTH_POS);
    }
}