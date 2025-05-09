using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaAnniSQLBuilder
{
    private static string WhereById()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessaTotale>();

        return string.Join(" AND ",
            $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}",
            $"{fieldProdotto} = @{nameof(@obj.Prodotto)}");
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessaTotale? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.AnnoValidita));
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessaTotale);
        tableName = tableName.GetTable<DatiModuloCommessaTotale>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select DISTINCT(/**select**/) from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }
    public static string SelectByAnnoMese()
    {
        return $@"
SELECT DISTINCT
    ([AnnoValidita]) as anno,
    [MeseValidita] as mese
FROM [pfw].[DatiModuloCommessa] ";
    }
    public static string OrderByAnnoMeseDesc()
    {
        return $@" ORDER BY  AnnoValidita DESC, MeseValidita DESC";
    }
}