using BenchmarkDotNet.Attributes;
using EvenireDB;
using EvenireDB.Common;
using System.Runtime.CompilerServices;
using System.Text;

[MemoryDiagnoser]
public class RawEventHeaderSerializationBenchmark
{
    private RawEventHeader[] _headers;
    private byte[] _buffer;

    [Params(10, 100, 1000, 10000)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[RawEventHeader.SIZE];

        _headers = new RawEventHeader[Length];
        for (int i = 0; i < Length; i++)
        {
            var eventType = $"event_{i}";
            var eventTypeBuff = new byte[Constants.MAX_EVENT_TYPE_LENGTH];
            Encoding.UTF8.GetBytes(eventType, eventTypeBuff);

            _headers[i] = new RawEventHeader(
                eventId: Guid.NewGuid(),
                eventType: eventTypeBuff,
                dataPosition: 42,
                eventDataLength: 71,
                eventTypeLength: (short)eventType.Length);
        }
    }

    [Benchmark(Baseline = true)]
    public void BitConverter_Copy()
    {
        for (int i = 0; i < Length; i++)
        {
            var header = _headers[i];

            Array.Copy(header.EventId.ToByteArray(), 0, _buffer, 0, TypeSizes.GUID);
            Array.Copy(BitConverter.GetBytes(header.DataPosition), 0, _buffer, 16, sizeof(long));
            Array.Copy(header.EventType, 0, _buffer, 24, Constants.MAX_EVENT_TYPE_LENGTH);
            Array.Copy(BitConverter.GetBytes(header.EventTypeLength), 0, _buffer, 74, sizeof(short));
            Array.Copy(BitConverter.GetBytes(header.EventDataLength), 0, _buffer, 76, sizeof(int));
        }
    }

    [Benchmark()]
    public void Unsafe_Copy()
    {
        for (int i = 0; i < Length; i++)
        {
            var header = _headers[i];

            Array.Copy(header.EventId.ToByteArray(), 0, _buffer, 0, TypeSizes.GUID);
            Unsafe.As<byte, long>(ref _buffer[16]) = header.DataPosition;
            Array.Copy(header.EventType, 0, _buffer, 24, Constants.MAX_EVENT_TYPE_LENGTH);
            Unsafe.As<byte, short>(ref _buffer[74]) = header.EventTypeLength;
            Unsafe.As<byte, int>(ref _buffer[76]) = header.EventDataLength;
        }
    }
}