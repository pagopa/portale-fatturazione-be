using Dapper;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public static class ProfiloSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        Ente? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Profilo).GetAsColumn<Ente>());
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(Ente);
        tableName = tableName.GetTable<Ente>();
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select DISTINCT /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}