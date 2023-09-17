internal struct RawEventIndexOffset
{
    public long EventIndex;
    public long Offset;

    public const int SIZE =
        sizeof(long) + // index        
        sizeof(long) // offset
    ;

    private const int EVENTINDEX_POS = 0;
    private const int OFFSET_POS = EVENTINDEX_POS + sizeof(long);

    public readonly void CopyTo(byte[] buffer)
    {
        // TODO: avoid BitConverter

        // event index
        Array.Copy(BitConverter.GetBytes(this.EventIndex), 0, buffer, EVENTINDEX_POS, sizeof(long));

        // offset
        Array.Copy(BitConverter.GetBytes(this.Offset), 0, buffer, OFFSET_POS, sizeof(long));
    }
}
