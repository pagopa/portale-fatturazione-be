using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;
public class TipoSpedizioneSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        TipoSpedizione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<TipoSpedizione>());
        builder.Select(nameof(@obj.Descrizione));
        builder.Select(nameof(@obj.IdCategoriaSpedizione).GetAsColumn<TipoSpedizione>());
        builder.Select(nameof(@obj.Tipo));
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(TipoSpedizione);
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}