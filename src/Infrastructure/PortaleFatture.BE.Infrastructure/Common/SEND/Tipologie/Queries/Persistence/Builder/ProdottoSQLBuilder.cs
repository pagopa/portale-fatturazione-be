using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

public static class ProdottoSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        Prodotto? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Nome).GetAsColumn<Prodotto>());
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(Prodotto);
        tableName = tableName.GetTable<Prodotto>();
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}