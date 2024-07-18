using Dapper;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence.Builder;

public class EnteSQLBuilder
{
    private static string WhereById()
    {
        Ente? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<Ente>(); 
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}";
    }

    private static SqlBuilder CreateSelect()
    {
        Ente? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Profilo).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Descrizione).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Email).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Address).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Cap).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.CodiceIstat).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Citta).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Provincia).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Nazione).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.PartitaIva).GetAsColumn<Ente>());
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(Ente);
        tableName = tableName.GetTable<Ente>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }
}
