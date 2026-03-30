using System.Text;
using Xunit;

namespace EvenireDB.Client.Tests;

public static class TestUtils
{
    public static EventData[] BuildEventsData(int count, string type = "lorem", int bytesCount = 100)
    {
        var data = Encoding.UTF8.GetBytes(new string('a', bytesCount));
        return Enumerable.Range(0, count)
                    .Select(i => new EventData(type, data))
                    .ToArray();
    }

    public static Event[] BuildEvents(int count, string type = "lorem", int bytesCount = 100)
    {
        var data = Encoding.UTF8.GetBytes(new string('a', bytesCount));
        return Enumerable.Range(0, count)
                    .Select(i => new Event(new EventId(i), type, data))
                    .ToArray();
    }

    public static bool IsEquivalent(EventData[] src, EventData[] other) 
    {
        Assert.NotNull(src);
        Assert.Equal(other.Length, src.Length);

        for (int i = 0; i < src.Length; i++)
   {
    Assert.Equal(other[i].Type, src[i].Type);
     Assert.Equal(other[i].Data.ToArray(), src[i].Data.ToArray());
     }

        return true;
    }
}