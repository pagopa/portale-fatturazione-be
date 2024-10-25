using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence.Builder;

public class ContrattoSQLBuilder
{
    private static string WhereById()
    {
        Contratto? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<Contratto>();
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<Contratto>();
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldProdotto} = @{nameof(@obj.Prodotto)}";
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
