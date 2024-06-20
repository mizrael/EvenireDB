using EvenireDB.Common;

public static class RoutingUtils
{
    public static string StreamDetails(StreamId streamId)
        => $"streams/{streamId.Type}/{streamId.Key}";
}
