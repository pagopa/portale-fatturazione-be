using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence.Builder;

public class ContrattoSQLBuilder
{
    private static string WhereById()
    {
        Contratto? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<Contratto>(); 
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}";
    }

    private static SqlBuilder CreateSelect()
    {
        Contratto? @obj = null;
        var builder = new SqlBuilder(); 
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<Contratto>()); 
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<Contratto>());
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<Contratto>());
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(Contratto);
        tableName = tableName.GetTable<Contratto>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }
}
