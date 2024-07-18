using Dapper;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public class CategoriaSpedizioneSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        CategoriaSpedizione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<CategoriaSpedizione>());
        builder.Select(nameof(@obj.Descrizione));
        builder.Select(nameof(@obj.Tipo));
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(CategoriaSpedizione);
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}