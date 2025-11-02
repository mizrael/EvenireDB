using System.Text;

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
        src.Should().NotBeNull()
            .And.HaveCount(other.Length);            

        for (int i = 0; i < src.Length; i++)
        {
            src[i].Type.Should().Be(other[i].Type);
            src[i].Data.ToArray().Should().BeEquivalentTo(other[i].Data.ToArray());
        }

        return true;
    }
}