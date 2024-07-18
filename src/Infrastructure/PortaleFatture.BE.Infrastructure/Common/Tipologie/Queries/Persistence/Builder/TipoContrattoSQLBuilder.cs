using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

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