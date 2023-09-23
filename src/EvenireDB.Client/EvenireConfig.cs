namespace EvenireDB.Client
{
    public record EvenireConfig
    {
        public EvenireConfig(Uri uri)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public Uri Uri { get; }
    }
}