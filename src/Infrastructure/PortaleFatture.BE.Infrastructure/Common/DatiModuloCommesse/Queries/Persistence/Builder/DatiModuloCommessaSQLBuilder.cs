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
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {fieldMese} = @{nameof(@obj.MeseValidita)} AND  {prodotto} = @{nameof(@obj.Prodotto)}";
    }

    private static string WhereByAnno()
    {
        DatiModuloCommessa? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessa>();
        var fieldAnno = nameof(@obj.AnnoValidita); 
        var prodotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessa>();
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)} AND {fieldAnno} = @{nameof(@obj.AnnoValidita)} AND  {prodotto} = @{nameof(@obj.Prodotto)}";
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

    public static string SelectByRicerca()
    {
        return $@"
WITH digitali (FkidEnte, description, vatnumber,FkProdotto, TipoSpedizione,NumeroNotificheNazionali, NumeroNotificheInternazionali, Anno, Mese)
AS
(
	select 
		[pfw].[DatiModuloCommessa].FkidEnte as 'identificativo SC', 
		[pfd].[Enti].description as 'ragione sociale ente', 
		[pfd].[Enti].vatnumber as 'codice fiscale',
		[pfw].[DatiModuloCommessa].FkProdotto as 'prodotto', 
		'digitale' as 'tipo spedizione',
		[pfw].[DatiModuloCommessa].NumeroNotificheNazionali as 'N. Notifiche NZ',  
		[pfw].[DatiModuloCommessa].NumeroNotificheInternazionali as 'N. Notifiche INT',
		[pfw].[DatiModuloCommessa].AnnoValidita,
		[pfw].[DatiModuloCommessa].[MeseValidita]
	from [pfw].[DatiModuloCommessa]
	inner join [pfd].[Enti] on [pfw].[DatiModuloCommessa].FkidEnte = [pfd].[Enti].internalistitutionid 
		and [pfw].[DatiModuloCommessa].FkidTipoSpedizione = '3'
    where pfw.DatiModuloCommessa.AnnoValidita = @Anno AND
	   pfw.DatiModuloCommessa.MeseValidita = @Mese  
)
, 
raccomandate (FkidEnte, description, vatnumber,FkProdotto, TipoSpedizione,NumeroNotificheNazionali, NumeroNotificheInternazionali, Anno, Mese)
AS
(
	select 
		[pfw].[DatiModuloCommessa].FkidEnte as 'identificativo SC', 
		[pfd].[Enti].description as 'ragione sociale ente', 
		[pfd].[Enti].vatnumber as 'codice fiscale',
		[pfw].[DatiModuloCommessa].FkProdotto as 'prodotto', 
		'analogico AR' as 'tipo spedizione',
		[pfw].[DatiModuloCommessa].NumeroNotificheNazionali as 'N. Notifiche NZ',  
		[pfw].[DatiModuloCommessa].NumeroNotificheInternazionali as 'N. Notifiche INT',
		[pfw].[DatiModuloCommessa].AnnoValidita,
		[pfw].[DatiModuloCommessa].[MeseValidita]
	from [pfw].[DatiModuloCommessa]
	inner join [pfd].[Enti] on [pfw].[DatiModuloCommessa].FkidEnte = [pfd].[Enti].internalistitutionid 
		and [pfw].[DatiModuloCommessa].FkidTipoSpedizione = '1'
    where pfw.DatiModuloCommessa.AnnoValidita = @Anno AND
	   pfw.DatiModuloCommessa.MeseValidita = @Mese  
),
raccomandate890 (FkidEnte, description, vatnumber,FkProdotto, TipoSpedizione,NumeroNotificheNazionali, NumeroNotificheInternazionali, Anno, Mese)
AS
(
	select 
		[pfw].[DatiModuloCommessa].FkidEnte as 'identificativo SC', 
		[pfd].[Enti].description as 'ragione sociale ente', 
		[pfd].[Enti].vatnumber as 'codice fiscale',
		[pfw].[DatiModuloCommessa].FkProdotto as 'prodotto', 
		'analogico 890' as 'tipo spedizione',
		[pfw].[DatiModuloCommessa].NumeroNotificheNazionali as 'N. Notifiche NZ',  
		[pfw].[DatiModuloCommessa].NumeroNotificheInternazionali as 'N. Notifiche INT',
		[pfw].[DatiModuloCommessa].AnnoValidita,
		[pfw].[DatiModuloCommessa].[MeseValidita]
	from [pfw].[DatiModuloCommessa]
	inner join [pfd].[Enti] on [pfw].[DatiModuloCommessa].FkidEnte = [pfd].[Enti].internalistitutionid 
		and [pfw].[DatiModuloCommessa].FkidTipoSpedizione = '2' 
    where pfw.DatiModuloCommessa.AnnoValidita = @Anno AND
	   pfw.DatiModuloCommessa.MeseValidita = @Mese  
),
TotaliEconomici (FkidEnte, CategoriaSpedizione, TotaleCategoria, Anno, Mese, Totale, IdTipoContratto)
AS
(
	SELECT [FkIdEnte], [FkIdCategoriaSpedizione], [TotaleCategoria], AnnoValidita, [MeseValidita], [Totale], [FkIdTipoContratto]
	FROM pfw.DatiModuloCommessaTotali
	where pfw.DatiModuloCommessaTotali.AnnoValidita = @Anno AND
	   pfw.DatiModuloCommessaTotali.MeseValidita = @Mese   
)
SELECT    d.FkidEnte as 'IdEnte'  
        , d.description as 'RagioneSociale'
		, d.vatnumber as 'CodiceFiscale'
		, d.FkProdotto as 'Prodotto'
		, d.TipoSpedizione as 'TipoSpedizioneDigitale'
		, d.NumeroNotificheNazionali as 'NumeroNotificheNazionaliDigitale'
		, d.NumeroNotificheInternazionali as 'NumeroNotificheInternazionaliDigitale'
		, r.TipoSpedizione  as 'TipoSpedizioneAnalogicoAR'
		, r.NumeroNotificheNazionali as 'NumeroNotificheNazionaliAnalogicoAR'
		, r.NumeroNotificheInternazionali as 'NumeroNotificheInternazionaliAnalogicoAR'
		, rr.TipoSpedizione  as 'TipoSpedizioneAnalogico890'
		, rr.NumeroNotificheNazionali as 'NumeroNotificheNazionaliAnalogico890'
		, rr.NumeroNotificheInternazionali as 'NumeroNotificheInternazionaliAnalogico890'
		, te.TotaleCategoria as 'TotaleCategoriaAnalogico'
		, te2.TotaleCategoria as 'TotaleCategoriaDigitale' 
		, te.Anno  as 'Anno'
        , te.Mese  As 'Mese'		
        , te.Totale as 'TotaleAnalogicoLordo'
		, te2.Totale as 'TotaleDigitaleLordo'
		, (te.Totale  + te2.Totale) as 'TotaleLordo'
		, te.IdTipoContratto as 'IdTipoContratto'


FROM digitali d
INNER JOIN raccomandate r on d.FkidEnte = r.FkidEnte
INNER JOIN raccomandate890 rr on r.FkidEnte = rr.FkidEnte
INNER JOIN TotaliEconomici te on r.FkidEnte = te.FkidEnte and te.CategoriaSpedizione = 1
INNER JOIN TotaliEconomici te2 on r.FkidEnte = te2.FkidEnte and te2.CategoriaSpedizione = 2 

WHERE d.Anno = @Anno and d.Mese = @Mese 
";
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