using System.Text;

namespace EvenireDB.Benchmark;

public static class Utils
{
    public static Event[] CreateEvents(int count, int minSize, int maxSize)
    => Enumerable.Range(0, count)
                .Select(i => new Event(new EventId(i, 0), "test",
                                        Encoding.UTF8.GetBytes(new string('x', Random.Shared.Next(minSize, maxSize)))))
                .ToArray();
    
    public static Client.EventData[] CreateClientEvents(int count, int minSize, int maxSize)
    => Enumerable.Range(0, count)
                .Select(i => Client.EventData.Create(new { id = i, data = new string('x', Random.Shared.Next(minSize, maxSize)) }))
                .ToArray();
}