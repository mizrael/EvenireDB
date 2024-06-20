using EvenireDB.Common;

namespace EvenireDB.Tests;

public class StreamTypeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void StreamType_should_throw_when_value_invalid(string value)
    {
        Action act = () => new StreamType(value);
        act.Should().Throw<ArgumentException>();
    }
}
