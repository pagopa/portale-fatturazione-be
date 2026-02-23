using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.ImportFattureGrandiAderenti.Extensions;

public class DecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDecimal();

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("F2")); // Always 2 decimals
} 