using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JetFlow.UI.Json;

internal class BigIntegerConverter
    : JsonConverter<BigInteger>
{
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Read raw number text if possible or use standard parsing
            if (reader.TryGetInt64(out long lVal)) return new BigInteger(lVal);
            return BigInteger.Parse(reader.GetString(), NumberFormatInfo.InvariantInfo);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString();
            if (BigInteger.TryParse(str, NumberFormatInfo.InvariantInfo, out BigInteger result))
            {
                return result;
            }
        }

        throw new JsonException("Unable to convert to BigInteger.");
    }

    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
    {
        // Writes as a raw unquoted number value
        writer.WriteRawValue(value.ToString(NumberFormatInfo.InvariantInfo), skipInputValidation: true);
    }
}
