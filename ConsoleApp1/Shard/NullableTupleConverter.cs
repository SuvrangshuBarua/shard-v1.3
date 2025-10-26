using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shard
{
    public class NullableTupleConverter : JsonConverter<(int Axis, bool Inverted)?>
    {
        public override (int Axis, bool Inverted)? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected JSON array for JoystickAxis.");

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException("Expected an integer for the Axis value.");

            int axis = reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
                throw new JsonException("Expected a boolean for the Inverted value.");

            bool inverted = reader.GetBoolean();

            reader.Read();

            return (axis, inverted);
        }

        public override void Write(Utf8JsonWriter writer, (int Axis, bool Inverted)? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            writer.WriteNumberValue(value.Value.Axis);
            writer.WriteBooleanValue(value.Value.Inverted);
            writer.WriteEndArray();
        }
    }
}