using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public class JsonTypeHandler : SqlMapper.ITypeHandler
{
    public void SetValue(IDbDataParameter parameter, object value)
    {
        parameter.Value = ((FattureListaDto)value).Serialize(); // inutile
    }

    public object Parse(Type destinationType, object value)
    {
        if (value is null || string.IsNullOrEmpty(value.ToString()) || value.ToString() == "{}")
            return new FattureListaDto();
        return value!.ToString()!.Deserialize<FattureListaDto>();
    }
}

public class BoolFromIntJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32() == 1;
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        return reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value ? 1 : 0);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}