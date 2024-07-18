using Dapper;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;

public static class DatiFatturazioneStoricoSQLBuilder
{
    private static string WhereByIdEnteAnnoMese()
    {
        DatiFatturazioneStorico? obj;
        var fieldIdEnteName = nameof(@obj.IdEnte).GetColumn<DatiFatturazioneStorico>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldMese = nameof(@obj.MeseValidita);
        return $"{fieldIdEnteName} = @{nameof(@obj.IdEnte)}  AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {fieldMese} = @{nameof(@obj.MeseValidita)}";
    }
 

    private static SqlBuilder CreateSelect()
    {
        DatiFatturazioneStorico? @obj = null;
        var builder = new SqlBuilder(); 
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.AnnoValidita));
        builder.Select(nameof(@obj.MeseValidita));
        builder.Select(nameof(@obj.DatiFatturazione).GetAsColumn<DatiFatturazione>()); 
        return builder;
    } 

    public static string SelectByIdEnteAnnoMese()
    { 
        var tableName = nameof(DatiFatturazione);
        var builder = CreateSelect();
        var where = WhereByIdEnteAnnoMese();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    } 
} 