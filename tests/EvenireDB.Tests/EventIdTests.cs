namespace EvenireDB.Tests;

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
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void Parse_should_work()
    {
        var eventId = EventId.Parse("42-71");
        Assert.Equal(42, eventId.Timestamp);
        Assert.Equal(71, eventId.Sequence);
    }
}