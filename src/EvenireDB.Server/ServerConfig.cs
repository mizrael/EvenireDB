internal record class ServerConfig
{
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(4);
    public int MaxPageSize { get; init; } = 100;
    public int MaxEventDataSize { get; init; } = 500_000;
    public string? DataFolder { get; init; }
}