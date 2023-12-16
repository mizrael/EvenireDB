namespace EvenireDB.Client.Tests
{
    public static class TestUtils
    {
        private readonly static byte[] _defaultEventData = new byte[] { 0x42 };

        public static EventData[] BuildEventsData(int count)
           => Enumerable.Range(0, count).Select(i => new EventData("lorem", _defaultEventData)).ToArray();

        public static Event[] BuildEvents(int count)
           => Enumerable.Range(0, count).Select(i => new Event(new EventId(i, 0), "lorem", _defaultEventData)).ToArray();

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
}