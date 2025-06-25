using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Extensions;
public static class SerializationExtensions
{
    public static readonly JsonSerializerOptions _options = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        IncludeFields = false,
        IgnoreReadOnlyProperties = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public static readonly JsonSerializerOptions _optionsReadOnly = new()
    {
        WriteIndented = false,
        IncludeFields = true,
        IgnoreReadOnlyProperties = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles 
    };

    public static string Serialize<T>(this T value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public static string SerializeAlsoReadOnly<T>(this T value)
    {
        return JsonSerializer.Serialize(value, _optionsReadOnly);
    }

    public static T DeserializeAlsoReadOnly<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, _optionsReadOnly)!;
    }

    public static T Deserialize<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options)!;
    } 
}