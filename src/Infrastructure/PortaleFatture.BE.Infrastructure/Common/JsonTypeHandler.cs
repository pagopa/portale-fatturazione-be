using System.Data;
using Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common;

public class JsonTypeHandler : SqlMapper.ITypeHandler
{
    public void SetValue(IDbDataParameter parameter, object value)
    {
        parameter.Value = ((FattureListaDto)value).Serialize(); // inutile
    }
  
    public object Parse(Type destinationType, object value)
    {
        if(value is null || String.IsNullOrEmpty(value.ToString()) || value.ToString() == "{}")
            return new FattureListaDto();
        return value!.ToString()!.Deserialize<FattureListaDto>(); 
    }
}