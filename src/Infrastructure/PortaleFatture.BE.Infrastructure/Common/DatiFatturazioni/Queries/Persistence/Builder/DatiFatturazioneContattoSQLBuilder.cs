using Dapper;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;

public static class DatiFatturazioneContattoSQLBuilder
{
    private static string WhereByIdDatiFatturazione()
    {
        DatiFatturazioneContatto? obj;
        var fieldId = nameof(@obj.IdDatiFatturazione).GetColumn<DatiFatturazioneContatto>();
        return $"{fieldId} = @{nameof(@obj.IdDatiFatturazione)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiFatturazioneContatto? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdDatiFatturazione).GetAsColumn<DatiFatturazioneContatto>());
        builder.Select(nameof(@obj.Email)); 
        return builder;
    }

    public static string SelectAllByIdDatiFatturazione()
    {
        var tableName = nameof(DatiFatturazioneContatto);
        tableName = tableName.GetTable<DatiFatturazioneContatto>();
        var builder = CreateSelect();
        var where = WhereByIdDatiFatturazione();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }
} 