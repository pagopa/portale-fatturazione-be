
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PortaleFatture.BE.Function.API.Extensions;


public class JsonIgnoreSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsClass || IsStruct(context.Type))
        {
            var excludedProperties = context.Type.GetProperties()
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                .Select(p => p.Name.ToLowerInvariant()); 

            foreach (var propertyName in excludedProperties)
            {
                if (schema.Properties.ContainsKey(propertyName))
                {
                    schema.Properties.Remove(propertyName);
                }
            }
        }
    }

    private bool IsStruct(Type type)
    {
        return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);
    }
}
