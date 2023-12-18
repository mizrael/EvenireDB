namespace EvenireDB.Tests
{
    public class EventIdTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("lorem-ipsum")]
        [InlineData("lorem-ipsum-dolor-sit-amet")]
        [InlineData("lorem-ipsum-dolor-sit-amet-")]
        [InlineData("-")]
        [InlineData(null)]
        public void Parse_should_throw_when_text_is_invalid(string text)
        {
            Action act = () => EventId.Parse(text);
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Parse_should_work()
        {
            var eventId = EventId.Parse("42-71");
            eventId.Timestamp.Should().Be(42);
            eventId.Sequence.Should().Be(71);
        }
    }
}