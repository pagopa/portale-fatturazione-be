using System.Text.Json.Serialization;
using System.Text.Json;

namespace PortaleFatture.BE.Infrastructure.Extensions;
public static class SerializationExtensions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true,
        IncludeFields = false,
        IgnoreReadOnlyProperties = true,
    };

    public static string Serialize<T>(this T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }

    public static T DeserializeFromDb<T>(this string json)
    {
        json = json.Replace(@"\""", @"""");
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }
}