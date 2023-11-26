﻿using Dapper;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaAnniSQLBuilder
{ 
    private static string WhereById()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>(); 
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessaTotale? @obj = null;
        var builder = new SqlBuilder();  
        builder.Select(nameof(@obj.AnnoValidita));  
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessaTotale);
        tableName = tableName.GetTable<DatiModuloCommessaTotale>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select DISTINCT(/**select**/) from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }
}