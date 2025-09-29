using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankSystem.Shared.WebApiDefaults.JsonConverters;

/// <summary>
/// Custom JSON converter for GUID that handles empty strings and null values
/// by converting them to Guid.Empty instead of throwing deserialization errors
/// </summary>
public class GuidJsonConverter : JsonConverter<Guid>
{
    public override Guid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();

                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return Guid.Empty;
                }

                // Try to parse the GUID string.
                // If parsing fails, return Guid.Empty (validation will catch this)
                return Guid.TryParse(stringValue, out var guid) ? guid : Guid.Empty;

            case JsonTokenType.Null:
                return Guid.Empty;

            default:
                throw new JsonException(
                    $"Unexpected token parsing Guid. Expected String or Null but got {reader.TokenType}."
                );
        }
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        // Always write as string representation
        writer.WriteStringValue(value.ToString());
    }
}
