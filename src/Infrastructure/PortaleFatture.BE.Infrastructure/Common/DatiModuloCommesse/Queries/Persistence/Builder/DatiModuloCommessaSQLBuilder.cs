using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaSQLBuilder
{
    private static string WhereById()
    {
        DatiModuloCommessa? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessa>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldMese = nameof(@obj.MeseValidita);
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {fieldMese} = @{nameof(@obj.MeseValidita)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessa? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.IdTipoSpedizione).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.MeseValidita));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica));
        builder.Select(nameof(@obj.AnnoValidita));
        builder.Select(nameof(@obj.NumeroNotificheInternazionali));
        builder.Select(nameof(@obj.NumeroNotificheNazionali));
        builder.Select(nameof(@obj.Stato).GetAsColumn<DatiModuloCommessa>());
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessa); 
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where); 
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    } 
}