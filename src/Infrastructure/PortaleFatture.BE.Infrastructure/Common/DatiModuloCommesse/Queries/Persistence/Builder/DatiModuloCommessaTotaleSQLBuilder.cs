using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaTotaleSQLBuilder
{
    private static string WhereById()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldMese = nameof(@obj.MeseValidita);
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {fieldMese} = @{nameof(@obj.MeseValidita)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessaTotale? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.IdCategoriaSpedizione).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.Stato).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.MeseValidita)); 
        builder.Select(nameof(@obj.AnnoValidita));
        builder.Select(nameof(@obj.TotaleCategoria)); 
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessaTotale); 
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where); 
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    } 
}