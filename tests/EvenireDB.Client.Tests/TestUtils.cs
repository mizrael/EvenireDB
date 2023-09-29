namespace EvenireDB.Client.Tests
{
    public static class TestUtils
    {
        private readonly static byte[] _defaultEventData = new byte[] { 0x42 };

        public static Event[] BuildEvents(int count)
           => Enumerable.Range(0, count).Select(i => new Event(Guid.NewGuid(), "lorem", _defaultEventData)).ToArray();
    }
}