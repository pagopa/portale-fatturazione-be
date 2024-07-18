using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiConfigurazioneModuloCommessaSQLBuilder
{
    private static string OrderByDataInizioValidita()
    {
        DatiConfigurazioneModuloTipoCommessa? obj;
        var fieldDataInizioValidita = nameof(@obj.DataInizioValidita); 
        return $"{fieldDataInizioValidita}";
    }

    private static string WhereByTipo()
    {
        DatiConfigurazioneModuloTipoCommessa? obj;
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiConfigurazioneModuloTipoCommessa>();
        var fieldTipoContratto = nameof(@obj.IdTipoContratto).GetColumn<DatiConfigurazioneModuloTipoCommessa>(); 
        return $"{fieldTipoContratto} = @{nameof(@obj.IdTipoContratto)} AND {fieldProdotto} = @{nameof(@obj.Prodotto)} AND {nameof(@obj.DataFineValidita)} IS NULL"; 
    }

    private static string WhereByCategoria()
    {
        DatiConfigurazioneModuloCategoriaCommessa? obj;
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiConfigurazioneModuloCategoriaCommessa>();
        var fieldTipoContratto = nameof(@obj.IdTipoContratto).GetColumn<DatiConfigurazioneModuloCategoriaCommessa>();
        return $"{fieldTipoContratto} = @{nameof(@obj.IdTipoContratto)} AND {fieldProdotto} = @{nameof(@obj.Prodotto)} AND {nameof(@obj.DataFineValidita)} IS NULL";
    }

    private static SqlBuilder CreateSelectTipo()
    {
        DatiConfigurazioneModuloTipoCommessa? @obj = null;
        var builder = new SqlBuilder(); 
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiConfigurazioneModuloTipoCommessa>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiConfigurazioneModuloTipoCommessa>());
        builder.Select(nameof(@obj.IdTipoSpedizione).GetAsColumn<DatiConfigurazioneModuloTipoCommessa>());
        builder.Select(nameof(@obj.MediaNotificaNazionale));
        builder.Select(nameof(@obj.MediaNotificaInternazionale));
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica));
        builder.Select(nameof(@obj.DataInizioValidita));
        builder.Select(nameof(@obj.DataFineValidita));
        builder.Select(nameof(@obj.Descrizione)); 
        return builder;
    }

    public static string SelectTipoBy()
    {
        var tableName = nameof(DatiConfigurazioneModuloTipoCommessa);
        tableName = tableName.GetTable<DatiConfigurazioneModuloTipoCommessa>();
        var builder = CreateSelectTipo();
        var where = WhereByTipo();
        builder.Where(where); 
        builder.OrderBy($"{OrderByDataInizioValidita()} DESC");
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ /**orderby**/");
        return builderTemplate.RawSql;
    }

    public static string SelectCategoriaBy()
    {
        var tableName = nameof(DatiConfigurazioneModuloCategoriaCommessa);
        tableName = tableName.GetTable<DatiConfigurazioneModuloCategoriaCommessa>();
        var builder = CreateSelectCategoria();
        var where = WhereByCategoria();
        builder.Where(where);
        builder.OrderBy($"{OrderByDataInizioValidita()} DESC");
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ /**orderby**/");
        return builderTemplate.RawSql;
    }
    private static SqlBuilder CreateSelectCategoria()
    {
        DatiConfigurazioneModuloCategoriaCommessa? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiConfigurazioneModuloCategoriaCommessa>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiConfigurazioneModuloCategoriaCommessa>());
        builder.Select(nameof(@obj.IdCategoriaSpedizione).GetAsColumn<DatiConfigurazioneModuloCategoriaCommessa>());
        builder.Select(nameof(@obj.Percentuale)); 
        builder.Select(nameof(@obj.DataCreazione));
        builder.Select(nameof(@obj.DataModifica));
        builder.Select(nameof(@obj.DataInizioValidita));
        builder.Select(nameof(@obj.DataFineValidita));
        builder.Select(nameof(@obj.Descrizione));
        return builder;
    }
} 