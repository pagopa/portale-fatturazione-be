namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

/// <summary>
/// Classe statica che fornisce i builder per le query SQL relative alle fatture sospese. 
/// Questa classe centralizza la definizione delle query SQL, facilitando la manutenzione e la riusabilità del codice. 
/// Le query sono costruite in modo da supportare parametri dinamici, consentendo di filtrare i risultati in base alle esigenze specifiche della query.
/// </summary>
public static class FattureSospeseQueryBuilder
{
    private static readonly string _sqlReportAndamentoCreditoSospeso = @"
        SELECT 
             [IdEnte]
            ,[RagioneSociale]
            ,[IdContratto]
            ,[TipoContratto]
            ,[TipologiaFattura]
            ,[NumFatturaSospesa]
            ,[TipoDocumento]
            ,[DataFattura]
            ,[Anno]
            ,[Mese]
            ,[ImponibileFattura]
            ,[CreditoCumulato]
            ,[RelNonFirmata]
            ,[TipoREL]
            ,[AnnoREL]
            ,[MeseREL]
        FROM [be].[vwReportAndamentoCreditoSospeso]
        WHERE 
            (@Anno IS NULL OR [Anno] = @Anno)
            AND (@Mese IS NULL OR [Mese] = @Mese)
            AND (@FilterByTipologia = 0 OR [TipologiaFattura] IN @TipologiaFattura)
        ORDER BY [IdEnte], [TipologiaFattura] DESC, [Anno] DESC, [Mese] DESC;
    ";

    /// <summary>
    /// Restituisce la query SQL per selezionare il report di andamento del credito sospeso, con filtri dinamici per anno, mese e tipologia di fattura.
    /// </summary>
    /// <returns></returns>
    public static string SelectReportAndamentoCreditoSospeso() => _sqlReportAndamentoCreditoSospeso;
}
