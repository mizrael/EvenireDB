internal record class ServerConfig
{
    /// <summary>
    /// max number of streams to cache in memory
    /// </summary>
    public uint CacheCapacity { get; init; } = 1000;

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

    public int HttpPort { get; init; } = 16281;
    public int GrpcPort { get; init; } = 16282;
}