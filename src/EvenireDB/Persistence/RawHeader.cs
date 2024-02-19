using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct RawHeader
{
    public RawHeader(
        long idTimestamp, 
        int idSequence, 
        short typeLen,
        long dataOffset,
        int dataLen)
    {
        IdTimestamp = idTimestamp;
        IdSequence = idSequence;
        TypeLength = typeLen;
        DataOffset = dataOffset;
        DataLength = dataLen;
    }

    public readonly long IdTimestamp;
    public readonly int IdSequence;
    public readonly short TypeLength;
    public readonly long DataOffset;
    public readonly int DataLength;
};
