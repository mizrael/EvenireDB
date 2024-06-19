using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvenireDB.Common;

internal class StreamTypeJsonConverter : JsonConverter<StreamType>
{
    public override StreamType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return new StreamType(value);
    }

    public override void Write(Utf8JsonWriter writer, StreamType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}