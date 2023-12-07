using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaSQLBuilder
{
    private static string WhereById()
    {
        DatiModuloCommessa? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessa>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldMese = nameof(@obj.MeseValidita);
        var prodotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessa>();
        var idTipoContratto = nameof(@obj.IdTipoContratto).GetColumn<DatiModuloCommessa>();
        return $"{idTipoContratto} = @{nameof(@obj.IdTipoContratto)} AND {fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {fieldMese} = @{nameof(@obj.MeseValidita)} AND  {prodotto} = @{nameof(@obj.Prodotto)}";
    }

    private static string WhereByAnno()
    {
        DatiModuloCommessa? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessa>();
        var fieldAnno = nameof(@obj.AnnoValidita); 
        var prodotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessa>();
        var idTipoContratto = nameof(@obj.IdTipoContratto).GetColumn<DatiModuloCommessa>();
        return $"{idTipoContratto} = @{nameof(@obj.IdTipoContratto)} AND {fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {prodotto} = @{nameof(@obj.Prodotto)}";
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessa? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.IdTipoSpedizione).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.MeseValidita));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica));
        builder.Select(nameof(@obj.AnnoValidita));
        builder.Select(nameof(@obj.NumeroNotificheInternazionali));
        builder.Select(nameof(@obj.NumeroNotificheNazionali));
        builder.Select(nameof(@obj.Stato).GetAsColumn<DatiModuloCommessa>());
        builder.Select(nameof(@obj.ValoreNazionali));
        builder.Select(nameof(@obj.PrezzoNazionali));
        builder.Select(nameof(@obj.ValoreInternazionali));
        builder.Select(nameof(@obj.PrezzoInternazionali)); 
        return builder;
    }

    private static SqlBuilder CreateSelectDate()
    {
        DatiModuloCommessa? @obj = null;
        var builder = new SqlBuilder(); 
        builder.Select(nameof(@obj.MeseValidita));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica)); 
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessa); 
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where); 
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByDate()
    {
        var tableName = nameof(DatiModuloCommessa);
        var builder = CreateSelectDate();
        var where = WhereByAnno();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByAnno()
    {
        var tableName = nameof(DatiModuloCommessa);
        var builder = CreateSelect();
        var where = WhereByAnno();
        builder.Where(where);
        builder.OrderBy($"{OrderByMeseValidita()} DESC");
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ /**orderby**/");
        return builderTemplate.RawSql;
    } 

    private static string OrderByMeseValidita()
    {
        DatiModuloCommessa? obj;
        var fieldMeseValidita = nameof(@obj.MeseValidita);
        return $"{fieldMeseValidita}";
    }
}