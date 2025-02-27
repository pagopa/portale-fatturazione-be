﻿using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaTotaleSQLBuilder
{
    private static string WhereByAnno()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessaTotale>();
        return string.Join(" AND ",
            $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}",
            $"{fieldAnno} = @{nameof(@obj.AnnoValidita)}",
            $"{fieldProdotto} = @{nameof(@obj.Prodotto)}");
    }
    private static string WhereById()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldMese = nameof(@obj.MeseValidita);
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessaTotale>();
        return string.Join(" AND ",
            $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}",
            $"{fieldAnno} = @{nameof(@obj.AnnoValidita)}",
            $"{fieldMese} = @{nameof(@obj.MeseValidita)}",
            $"{fieldProdotto} = @{nameof(@obj.Prodotto)}");
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessaTotale? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.IdCategoriaSpedizione).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.Stato).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.MeseValidita));
        builder.Select(nameof(@obj.AnnoValidita));
        builder.Select(nameof(@obj.TotaleCategoria));
        builder.Select(nameof(@obj.PercentualeCategoria));
        builder.Select(nameof(@obj.Totale));
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessaTotale);
        tableName = tableName.GetTable<DatiModuloCommessaTotale>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByAnno()
    {
        var tableName = nameof(DatiModuloCommessaTotale);
        tableName = tableName.GetTable<DatiModuloCommessaTotale>();
        var builder = CreateSelect();
        var where = WhereByAnno();
        builder.Where(where);
        builder.OrderBy($"{OrderByMeseValidita()} DESC");
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ /**orderby**/");
        return builderTemplate.RawSql;
    }

    private static string OrderByMeseValidita()
    {
        DatiModuloCommessaTotale? obj;
        var fieldMeseValidita = nameof(@obj.MeseValidita);
        return $"{fieldMeseValidita}";
    }
}