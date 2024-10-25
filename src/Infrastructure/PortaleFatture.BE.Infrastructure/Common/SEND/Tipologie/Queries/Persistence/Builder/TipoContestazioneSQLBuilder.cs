using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

public static class TipoContestazioneSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        TipoContestazione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<TipoContestazione>());
        builder.Select(nameof(@obj.Tipo).GetAsColumn<TipoContestazione>());
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(TipoContestazione);
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}