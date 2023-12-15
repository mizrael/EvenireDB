namespace EvenireDB
{
    public record EventData
    {
        public EventData(string type, ReadOnlyMemory<byte> data)
        {
            Type = type;
            Data = data;
        }       

        public string Type { get; }
        public ReadOnlyMemory<byte> Data { get; }
    }
}