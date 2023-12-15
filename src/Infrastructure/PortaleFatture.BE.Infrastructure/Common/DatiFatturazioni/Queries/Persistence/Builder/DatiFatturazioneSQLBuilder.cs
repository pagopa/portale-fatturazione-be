using Dapper;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;

public static class DatiFatturazioneSQLBuilder
{
    private static string WhereByIdEnte()
    {
        DatiFatturazione? obj;
        var fieldIdEnteName = nameof(@obj.IdEnte).GetColumn<DatiFatturazione>();
        return $"{fieldIdEnteName} = @{nameof(@obj.IdEnte)}";
    }

    private static string WhereById()
    {
        DatiFatturazione? obj;
        var fieldId = nameof(@obj.Id).GetColumn<DatiFatturazione>();
        return $"{fieldId} = @{nameof(@obj.Id)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiFatturazione? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.Id).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.Cup));
        builder.Select(nameof(@obj.NotaLegale));
        builder.Select(nameof(@obj.CodCommessa));
        builder.Select(nameof(@obj.DataDocumento));
        builder.Select(nameof(@obj.SplitPayment));
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.IdDocumento));
        builder.Select(nameof(@obj.Map));
        builder.Select(nameof(@obj.TipoCommessa).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiFatturazione>());
        builder.Select(nameof(@obj.Pec));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica)); 
        return builder;
    }

    public static string SelectById()
    {
        var tableName = nameof(DatiFatturazione);
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByIdEnte()
    { 
        var tableName = nameof(DatiFatturazione);
        var builder = CreateSelect();
        var where =  WhereByIdEnte();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    } 
} 