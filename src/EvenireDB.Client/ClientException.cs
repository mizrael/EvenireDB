namespace EvenireDB.Client
{
    public class ClientException : Exception
    {
        public ClientException(int code, string message) : base(message)
        {
            Code = code;
        }

        public int Code { get; }
    }
}