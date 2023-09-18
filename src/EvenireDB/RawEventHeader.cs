internal struct RawEventHeader
{
    public Guid EventId;
    public int EventTypeNameLength; 
    public int EventDataLength;    

    public const int SIZE =
        TypeSizes.GUID + // index
        sizeof(int) + // type name length
        sizeof(int) // data length
    ;

    private const int EVENTID_POS = 0;
    private const int EVENTTYPENAME_POS = EVENTID_POS + TypeSizes.GUID;
    private const int EVENTDATALENGTH_POS = EVENTTYPENAME_POS + sizeof(int);

    public readonly void CopyTo(byte[] buffer)
    {
        // TODO: avoid BitConverter
        
        // event index
        Array.Copy(this.EventId.ToByteArray(), 0, buffer, EVENTID_POS, TypeSizes.GUID);

        // event type length
        Array.Copy(BitConverter.GetBytes(this.EventTypeNameLength), 0, buffer, EVENTTYPENAME_POS, sizeof(int));

        // event data length
        Array.Copy(BitConverter.GetBytes(this.EventDataLength), 0, buffer, EVENTDATALENGTH_POS, sizeof(int));
    }

    public static void Parse(byte[] data, ref RawEventHeader header)
    {
        header.EventId = new Guid(data.AsSpan(EVENTID_POS, TypeSizes.GUID));
        header.EventTypeNameLength = BitConverter.ToInt32(data, EVENTTYPENAME_POS);
        header.EventDataLength = BitConverter.ToInt32(data, EVENTDATALENGTH_POS);
    }    
}
