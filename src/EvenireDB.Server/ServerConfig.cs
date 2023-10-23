internal record class ServerConfig
{
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

    /// <summary>
    /// max number of streams to cache in memory
    /// </summary>
    public uint MaxInMemoryStreamsCount { get; init; } = 1000;

    /// <summary>
    /// max allowed memory allocated by the process. If exceeded, the system will try to recover
    /// some memory by dropping some cached streams
    /// </summary>
    public long MaxAllowedAllocatedBytes { get; init; } = 1_000_000_000; // TODO: consider making this a function of max allowed streams count and max event data size

    public TimeSpan MemoryWatcherInterval { get; init; } = TimeSpan.FromMinutes(2);
}