using EvenireDB.Common;
using System.Text;

namespace EvenireDB.Tests
{
    public class RawEventHeaderTests
    {
        [Fact]
        public void serialization_should_work()
        {
            var eventTypeData = new byte[Constants.MAX_EVENT_TYPE_LENGTH];
            var eventType = Encoding.UTF8.GetBytes("lorem");
            Array.Copy(eventType, eventTypeData, eventType.Length);

            var eventId = new Guid("54ECC541-0899-4E38-A2E3-6BC8C3258DC7");

            var header = new RawEventHeader(
                eventId: eventId,
                dataPosition: 42,
                eventDataLength: 71,
                eventType: eventTypeData,
                eventTypeLength: (short)eventType.Length
            );

            var destBuffer = new byte[RawEventHeader.SIZE];
            header.ToBytes(ref destBuffer);

            var deserializedHeader = new RawEventHeader(new ReadOnlySpan<byte>(destBuffer));
            deserializedHeader.DataPosition.Should().Be(42);
            deserializedHeader.EventId.Should().Be(eventId);
            deserializedHeader.EventDataLength.Should().Be(71);
            deserializedHeader.EventTypeLength.Should().Be((short)eventType.Length);
            deserializedHeader.EventType.Should().BeEquivalentTo(eventTypeData);
        }
    }
}