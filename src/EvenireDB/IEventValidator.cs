namespace EvenireDB
{
    public interface IEventValidator
    {
        void Validate(string type, ReadOnlyMemory<byte> data);
    }
}