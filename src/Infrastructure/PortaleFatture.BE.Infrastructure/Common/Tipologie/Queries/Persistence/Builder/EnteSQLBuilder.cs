using System.Text;
using Dapper;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public static class EnteSQLBuilder
{
    private static SqlBuilder CreateSelect()
    {
        Ente? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Descrizione).GetAsColumn<Ente>());
        return builder;
    }

    private static SqlBuilder CreateSelectWithId()
    {
        Ente? @obj = null;
        var builder = new SqlBuilder();
        builder.Select($"e.{nameof(@obj.IdEnte).GetAsColumn<Ente>()}");
        builder.Select(nameof(@obj.Descrizione).GetAsColumn<Ente>());
        return builder;
    }

    private static string WhereBySearch()
    {
        Ente? obj;
        var fieldDescription = nameof(@obj.Descrizione).GetColumn<Ente>();
        return $"{fieldDescription} LIKE '%' + @{nameof(@obj.Descrizione)} + '%'";
    }

    public static string AddSearch(string? prodotto, string? profilo)
    {
        var stringBuilder = new StringBuilder();
        Ente? obj;
        if (!string.IsNullOrWhiteSpace(profilo))
        {
            stringBuilder.Append(" AND ");
            var fieldProfilo = nameof(@obj.Profilo).GetColumn<Ente>();
            stringBuilder.Append($"{fieldProfilo} = @{nameof(@obj.Profilo)}");
        }
      
        if (!string.IsNullOrWhiteSpace(prodotto))
        {
            stringBuilder.Append(" AND ");
            var fieldProdotto= nameof(Contratto.Prodotto).GetColumn<Contratto>();
            stringBuilder.Append($"{fieldProdotto} = @{nameof(Contratto.Prodotto)}");
        }
        return stringBuilder.ToString();
    }

    private static int _top = 100;
    public static string SelectAllBySearch()
    {
        var tableName = $"[schema]{nameof(Ente).GetTable<Ente>()} e";
        var builder = CreateSelect();

        var rightEnteTable = $"[schema]{nameof(Ente).GetTable<Ente>()}";
        var innerContrattoTable = $"[schema]{nameof(Contratto).GetTable<Contratto>()}";
        var internalIdEnte = nameof(Ente.IdEnte).GetColumn<Ente>();
        builder.InnerJoin($"{innerContrattoTable} c on e.{internalIdEnte} = c.{internalIdEnte}"); 

        builder.Where(WhereBySearch());

        var builderTemplate = builder.AddTemplate($"Select TOP {_top} /**select**/ from {tableName} /**innerjoin**/ /**where**/ ");
        return builderTemplate.RawSql;
    }
    public static string SelectAllByDescrizione()
    {
        var tableName = $"[schema]{nameof(Ente).GetTable<Ente>()} e";
        var builder = CreateSelectWithId();

        var rightEnteTable = $"[schema]{nameof(Ente).GetTable<Ente>()}";
        var innerContrattoTable = $"[schema]{nameof(Contratto).GetTable<Contratto>()}";
        var internalIdEnte = nameof(Ente.IdEnte).GetColumn<Ente>();
        builder.InnerJoin($"{innerContrattoTable} c on e.{internalIdEnte} = c.{internalIdEnte}");

        builder.Where(WhereBySearch());

        var builderTemplate = builder.AddTemplate($"Select TOP {_top} /**select**/ from {tableName} /**innerjoin**/ /**where**/ ");
        return builderTemplate.RawSql;
    }
}