internal struct RawEventOffsetIndex
{
    public Guid EventId;
    public long MainStreamPosition;
    public long EventDataSize;

    public const int SIZE =
        TypeSizes.GUID + // index        
        sizeof(long) + // offset in the main stream
        sizeof(long) // event data size
    ;

    private const int EVENTID_POS = 0;
    private const int OFFSET_POS = EVENTID_POS + TypeSizes.GUID;
    private const int DATA_POS = OFFSET_POS + sizeof(long);

    public readonly void CopyTo(byte[] buffer)
    {
        // TODO: avoid BitConverter

        // event index
        Array.Copy(this.EventId.ToByteArray(), 0, buffer, EVENTID_POS, TypeSizes.GUID);

        // offset in the main stream
        Array.Copy(BitConverter.GetBytes(this.MainStreamPosition), 0, buffer, OFFSET_POS, sizeof(long));

        // event data size
        Array.Copy(BitConverter.GetBytes(this.EventDataSize), 0, buffer, DATA_POS, sizeof(long));
    }

    public static void Parse(byte[] data, ref RawEventOffsetIndex header)
    {
        header.EventId = new Guid(data.AsSpan(EVENTID_POS, TypeSizes.GUID));
        header.MainStreamPosition = BitConverter.ToInt32(data, OFFSET_POS);
        header.EventDataSize = BitConverter.ToInt32(data, DATA_POS);
    }
}
