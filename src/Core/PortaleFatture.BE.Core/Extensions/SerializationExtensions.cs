using System.Text.Json;

namespace PortaleFatture.BE.Core.Extensions;
public static class SerializationExtensions
{
    public static readonly JsonSerializerOptions _options = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        IncludeFields = false
    };

    public static string Serialize<T>(this T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public static T Deserialize<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }
}