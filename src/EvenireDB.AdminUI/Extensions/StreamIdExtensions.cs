using EvenireDB.Common;

public static class StreamIdExtensions
{
    public static string ToFriendlyString(this StreamId streamId)
        => $"{streamId.Type}/{streamId.Key}";
}