using Dapper;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Core.Entities.Utenti;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.Queries.Persistence.Builder;

public class UtenteSQLBuilder
{
    private static string WhereById()
    {
        Utente? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<Ente>();
        var fieldIdUtente = nameof(@obj.IdUtente);
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldIdUtente} = @{nameof(@obj.IdUtente)} ";
    }

    private static SqlBuilder CreateSelect()
    {
        Utente? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdUtente));
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<Utente>());
        builder.Select(nameof(@obj.DataUltimo));
        builder.Select(nameof(@obj.DataPrimo));
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(Utente);
        tableName = tableName.GetTable<Utente>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }
}
