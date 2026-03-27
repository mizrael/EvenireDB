using EvenireDB.Common;

namespace EvenireDB.Tests;

public class EventIdGeneratorTests
{
    [Fact]
    public void Generate_should_create_id_with_current_timestamp_when_no_previous()
    {
        var fixedTime = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(fixedTime);

        var sut = new EventIdGenerator(timeProvider);
        var result = sut.Generate();

        Assert.Equal(fixedTime.UtcTicks, result.Timestamp);
        Assert.Equal(0, result.Sequence);
    }

    [Fact]
    public void Generate_should_increment_sequence_when_timestamp_matches_previous()
    {
        var fixedTime = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(fixedTime);

        var sut = new EventIdGenerator(timeProvider);
        var previous = new EventId(fixedTime.UtcTicks, 5);
        var result = sut.Generate(previous);

        Assert.Equal(fixedTime.UtcTicks, result.Timestamp);
        Assert.Equal(6, result.Sequence);
    }

    [Fact]
    public void Generate_should_increment_sequence_when_previous_timestamp_is_ahead()
    {
        var earlyTime = new DateTimeOffset(2025, 1, 15, 10, 29, 0, TimeSpan.Zero);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(earlyTime);

        var sut = new EventIdGenerator(timeProvider);
        var previous = new EventId(earlyTime.UtcTicks + 1000, 3);
        var result = sut.Generate(previous);

        Assert.Equal(4, result.Sequence);
    }

    [Fact]
    public void Generate_should_preserve_sequence_when_timestamp_advances()
    {
        var time1 = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var time2 = time1.AddMilliseconds(1);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(time2);

        var sut = new EventIdGenerator(timeProvider);
        var previous = new EventId(time1.UtcTicks, 42);
        var result = sut.Generate(previous);

        Assert.Equal(time2.UtcTicks, result.Timestamp);
        Assert.Equal(42, result.Sequence);
    }

    [Fact]
    public void Generate_should_produce_monotonically_increasing_ids()
    {
        var baseTime = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(baseTime);

        var sut = new EventIdGenerator(timeProvider);

        EventId? previous = null;
        for (int i = 0; i < 100; i++)
        {
            var current = sut.Generate(previous);
            if (previous.HasValue)
            {
                bool isOrdered = current.Timestamp > previous.Value.Timestamp ||
                    (current.Timestamp == previous.Value.Timestamp && current.Sequence > previous.Value.Sequence);
                Assert.True(isOrdered, $"ID at index {i} is not ordered");
            }
            previous = current;
        }
    }
}
