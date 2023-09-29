internal record class ServerConfig
{
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(4);
    
    /// <summary>
    /// max page size returned to clients
    /// </summary>
    public uint MaxPageSizeToClient { get; init; } = 100;

    /// <summary>
    /// max page size when parsing events from disk
    /// </summary>
    public uint MaxEventsPageSizeFromDisk { get; init; } = 100;
    
    /// <summary>
    /// max allowed event data size
    /// </summary>
    public uint MaxEventDataSize { get; init; } = 500_000;

    public string? DataFolder { get; init; }
    public int Port { get; init; } = 16281;
}