using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaAnniSQLBuilder
{ 
    private static string WhereById()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldidTipoContratto = nameof(@obj.IdTipoContratto).GetColumn<DatiModuloCommessaTotale>();
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessaTotale>();

        return String.Join(" AND ",
            $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}", 
            $"{fieldidTipoContratto} = @{nameof(@obj.IdTipoContratto)}",
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
}