using EvenireDB.Common;

namespace EvenireDB.Tests
{
    public class EventValidatorTests
    {
        [Fact]
        public void Validate_should_fail_when_type_null()
        {
            var sut = new EventDataValidator(1024);
            var ex = Assert.Throws<ArgumentException>(() => sut.Validate(null, new byte[] { 0x42 }));
            ex.ParamName.Should().Be("type");
        }

        [Fact]
        public void Validate_should_fail_when_type_invalid()
        {
            var type = new string('a', Constants.MAX_EVENT_TYPE_LENGTH + 1);
            var sut = new EventDataValidator(1024);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sut.Validate(type, new byte[] { 0x42 }));
            ex.ParamName.Should().Be("type");
        }

        [Fact]
        public void Validate_should_fail_when_data_null()
        {
            var sut = new EventDataValidator(1024);
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Validate("lorem", null));
        }

        [Fact]
        public void Validate_should_fail_when_data_empty()
        {
            var sut = new EventDataValidator(1024);
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Validate("lorem", Array.Empty<byte>()));
        }

        [Fact]
        public void Validate_should_fail_when_data_too_big()
        {
            var sut = new EventDataValidator(1024);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sut.Validate("lorem", new byte[1025]));
            ex.ParamName.Should().Be("data");
        }
    }
}
