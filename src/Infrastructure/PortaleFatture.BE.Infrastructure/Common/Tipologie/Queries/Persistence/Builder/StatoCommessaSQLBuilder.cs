using Dapper;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public class StatoCommessaSQLBuilder
{
    private static string WhereById()
    {
        StatoCommessa? obj;
        var fieldIdEnte = nameof(@obj.Default);
        return $"[{fieldIdEnte}] = 1";
    } 

    private static SqlBuilder CreateSelect()
    {
        StatoCommessa? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Stato));
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(StatoCommessa);
        tableName = tableName.GetTable<StatoCommessa>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName}  /**where**/ ");
        return builderTemplate.RawSql;
    }
} 