namespace EvenireDB.Client.Tests
{
    public static class TestUtils
    {
        private readonly static byte[] _defaultEventData = new byte[] { 0x42 };

        public static PersistedEvent[] BuildEvents(int count)
           => Enumerable.Range(0, count).Select(i => new Event(Guid.NewGuid(), "lorem", _defaultEventData)).ToArray();

        public static bool IsEquivalent(PersistedEvent[] src, PersistedEvent[] other) 
        {
            src.Should().NotBeNull()
                .And.HaveCount(other.Length);            

            for (int i = 0; i < src.Length; i++)
            {
                src[i].Id.Should().Be(other[i].Id);
                src[i].Type.Should().Be(other[i].Type);
                src[i].Data.ToArray().Should().BeEquivalentTo(other[i].Data.ToArray());
            }

            return true;
        }
    }
}