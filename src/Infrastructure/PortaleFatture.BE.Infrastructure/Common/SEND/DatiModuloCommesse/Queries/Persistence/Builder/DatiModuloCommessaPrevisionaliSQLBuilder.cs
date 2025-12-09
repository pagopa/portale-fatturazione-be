namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

public static class DatiModuloCommessaPrevisionaliSQLBuilder
{
    public static string SelectPrevisionaleByAnnoMese()
    {
        return $@"
SELECT  [annoRiferimento]
      ,[meseRiferimento]
      ,[Source]
      ,[Year]
      ,[Month]
      ,[Quarter]
      ,[datavalidita]
      ,[datavaliditalegale]
  FROM [pfd].[vConfigurazioneDatiModuloCommessa]
  WHERE annoRiferimento=@annoriferimento AND meseRiferimento=@meseriferimento";
    }

    private static string _selectPrevisionaleByAnnoMeseIdEnte = $@"
SELECT [Anno]
      ,[Mese]
      ,[IdEnte]
      ,[Ente]
      ,[TipoReport]
      ,[TotaleModuloCommessa]
      ,[AR]
      ,[890] as A890
      ,[TotaleRegioni]
      ,[Regione]
      ,[Calcolato]
      ,[AR_REGIONI_PERC] as ArRegioniPerc
      ,[890_REGIONI_PERC] as Regioni890Perc
      ,[TOTALE_REGIONI_PERC] as TotaleRegioniPerc
      ,[TotaleCoperturaRegionale]
	  ,tc.Descrizione as TipologiaContratto
FROM [pfd].[vModuloCommessaPrevisionale_V2] v 
left outer join pfd.enti e on e.InternalIstitutionId = v.IdEnte
inner join pfd.Contratti c on c.internalistitutionid  = e.InternalIstitutionId
inner join pfw.TipoContratto tc on tc.IdTipoContratto = c.FkIdTipoContratto
WHERE (@idTipoContratto IS NULL OR c.FkIdTipoContratto = @idTipoContratto)
AND anno=@anno AND mese=@mese 
";

    private static string _orderbyAnnoMeseIdTipoReportEnte = $@"
  ORDER BY Anno, Mese, IdEnte,TipoReport DESC;
";

    public static string SelectPrevisionaleByAnnoMeseIdEnte()
    {
        return _selectPrevisionaleByAnnoMeseIdEnte;
    }
    public static string OrderbyAnnoMeseIdTipoReportEnte()
    {
        return _orderbyAnnoMeseIdTipoReportEnte;
    }
}