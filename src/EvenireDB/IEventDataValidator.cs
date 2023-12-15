namespace EvenireDB
{
    public interface IEventDataValidator
    {
        void Validate(string type, ReadOnlyMemory<byte> data);
    }
}