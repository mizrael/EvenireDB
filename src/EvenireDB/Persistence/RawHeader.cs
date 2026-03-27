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

    private readonly byte _padding1;
    private readonly byte _padding2;
    private readonly byte _padding3;
    private readonly byte _padding4;
    private readonly byte _padding5;
    private readonly byte _padding6;
};
