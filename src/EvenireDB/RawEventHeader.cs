internal struct RawEventHeader
{
    public long EventIndex;
    public int EventTypeNameLength; 
    public int EventDataLength;    

    public const int SIZE =        
        sizeof(long) + // index
        sizeof(int) + // type name length
        sizeof(int) // data length
    ;

    private const int EVENTINDEX_POS = 0;
    private const int EVENTTYPENAME_POS = EVENTINDEX_POS + sizeof(long);
    private const int EVENTDATALENGTH_POS = EVENTTYPENAME_POS + sizeof(int);

    public readonly void CopyTo(byte[] buffer)
    {
        // TODO: avoid BitConverter
        
        // event index
        Array.Copy(BitConverter.GetBytes(this.EventIndex), 0, buffer, EVENTINDEX_POS, sizeof(long));

        // event type length
        Array.Copy(BitConverter.GetBytes(this.EventTypeNameLength), 0, buffer, EVENTTYPENAME_POS, sizeof(int));

        // event data length
        Array.Copy(BitConverter.GetBytes(this.EventDataLength), 0, buffer, EVENTDATALENGTH_POS, sizeof(int));
    }

    public static void Parse(byte[] data, ref RawEventHeader header)
    {
        header.EventIndex = BitConverter.ToInt64(data, EVENTINDEX_POS);
        header.EventTypeNameLength = BitConverter.ToInt32(data, EVENTTYPENAME_POS);
        header.EventDataLength = BitConverter.ToInt32(data, EVENTDATALENGTH_POS);
    }    
}