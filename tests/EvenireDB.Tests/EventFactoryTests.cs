namespace EvenireDB.Tests
{
    public class EventFactoryTests
    {
        [Fact]
        public void Create_should_fail_when_type_null()
        {
            var sut = new EventFactory(1024);
            var ex = Assert.Throws<ArgumentException>(() => sut.Create(Guid.NewGuid(), null, new byte[] { 0x42 }));
            ex.ParamName.Should().Be("type");
        }

        [Fact]
        public void Create_should_fail_when_type_invalid()
        {
            var type = new string('a', Constants.MAX_EVENT_TYPE_LENGTH + 1);
            var sut = new EventFactory(1024);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sut.Create(Guid.NewGuid(), type, new byte[] { 0x42 }));
            ex.ParamName.Should().Be("type");
        }

        [Fact]
        public void Create_should_fail_when_data_null()
        {
            var sut = new EventFactory(1024);
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Create(Guid.NewGuid(), "lorem", null));
        }

        [Fact]
        public void Create_should_fail_when_data_empty()
        {
            var sut = new EventFactory(1024);
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Create(Guid.NewGuid(), "lorem", Array.Empty<byte>()));
        }

        [Fact]
        public void Create_should_fail_when_data_too_big()
        {
            var sut = new EventFactory(1024);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => sut.Create(Guid.NewGuid(), "lorem", new byte[1025]));
            ex.ParamName.Should().Be("data");
        }
    }
}
