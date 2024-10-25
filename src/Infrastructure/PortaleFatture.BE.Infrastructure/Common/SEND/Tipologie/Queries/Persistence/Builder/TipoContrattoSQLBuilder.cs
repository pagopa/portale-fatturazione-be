using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

public static class TipoContrattoSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        TipoContratto? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<TipoContratto>());
        builder.Select(nameof(@obj.Descrizione));
        return builder;
    }

    public static string SelectAll()
    {
        var tableName = nameof(TipoContratto);
        tableName = tableName.GetTable<TipoContratto>();
        var builder = CreateSelect();
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}");
        return builderTemplate.RawSql;
    }
}