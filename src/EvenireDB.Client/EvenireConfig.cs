namespace EvenireDB.Client
{
    public record EvenireConfig
    {
        public EvenireConfig(Uri uri, bool useGrpc)
        {
            this.Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.UseGrpc = useGrpc;  
        }

        public Uri Uri { get; }

        public bool UseGrpc { get; } = true;
    }
}