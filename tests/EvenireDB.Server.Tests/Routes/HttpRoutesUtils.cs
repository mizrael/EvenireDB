using EvenireDB.Server.DTO;

namespace EvenireDB.Server.Tests.Routes;

internal static class HttpRoutesUtils
{
    public readonly static byte[] DefaultEventData = new byte[] { 0x42 };

    public static EventDataDTO[] BuildEventsDTOs(int count, byte[]? data)
       => Enumerable.Range(0, count).Select(i => new EventDataDTO("lorem", data)).ToArray();
}