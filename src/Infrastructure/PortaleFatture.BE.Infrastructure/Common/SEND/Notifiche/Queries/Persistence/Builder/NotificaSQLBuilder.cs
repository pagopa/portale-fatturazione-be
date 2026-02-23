namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

internal static class NotificaSQLBuilder
{
    // non metto LEFT JOIN pfd.Notifiche_CodiceOggetto g ON n.event_id = g.event_id
    private static string _sqlCount = @"
SELECT Count(n.internal_organization_id) 
FROM pfd.[Notifiche] n
INNER JOIN pfd.Enti e ON e.InternalIstitutionId = n.internal_organization_id
INNER JOIN pfd.Contratti c ON e.InternalIstitutionId = c.internalistitutionid
LEFT JOIN pfw.Contestazioni t ON t.FkIdNotifica = n.event_id
INNER JOIN pfw.FlagContestazione f ON f.IdFlagContestazione = ISNULL(t.FkIdFlagContestazione, 1)
LEFT JOIN pfw.TipoContestazione a ON a.IdTipoContestazione = t.FkIdTipoContestazione
";
    private static string _sql = @"
SELECT [contract_id] AS IdContratto,
       [tax_code] AS CodiceFiscale,
       [vat_number] AS PIva,
       [zip_code] AS CAP,
       [foreign_state] AS StatoEstero,
       [number_of_pages] AS NumberOfPages,
       [g_envelope_weight] AS GEnvelopeWeight,
       [cost_eurocent] AS CostEuroInCentesimi,
       [timeline_category] AS TimelineCategory,
       [notificationtype] AS TipoNotifica,
       ISNULL([paper_product_type], 'DIG') as NotificationType,
       n.[event_id] AS IdNotifica,
       n.[iun] AS Iun,
       [notification_sent_at] AS DataInvio ,
       [internal_organization_id] AS IdEnte ,
       [event_timestamp] AS DATA ,
       [recipient_index] AS RecipientIndex,
       [recipient_type] AS RecipientType,
       [recipient_id] AS RecipientId,
       n.[year] AS anno,
       n.[month] AS mese,
       n.[daily] AS AnnoMeseGiorno,
       [item_code] AS ItemCode,
       [notification_request_id] AS NotificationRequestId,
       [recipient_tax_id] AS RecipientTaxId,
       [Recapitista] AS Recapitista,
       [Consolidatore] AS Consolidatore,
	   g.CodiceOggetto as CodiceOggetto,
       e.description AS RagioneSociale,
       e.institutionType AS Profilo,
       c.product AS Prodotto,
       f.FlagContestazione AS Contestazione,
       f.IdFlagContestazione AS StatoContestazione,
       ISNULL(a.TipoContestazione, f.FlagContestazione) AS TipoContestazione,
	   a.IdTipoContestazione AS IdTipoContestazione,
       n.Fatturabile as Fatturata,
 		   CAST(
             CASE
                  WHEN  n.[TipologiaFattura] = 'ASSEVERAZIONE'
                     THEN 'Notifica di ente aderente al bando PNRR in cui è prevista la fase di asseverazione'
                  ELSE n.[TipologiaFattura] 
             END AS nvarchar(300)) as TipologiaFattura,
       t.onere AS Onere,
       t.NoteEnte AS NoteEnte,
	   t.RispostaEnte AS RispostaEnte,
       t.NoteSend AS NoteSend,
       t.NoteRecapitista AS NoteRecapitista,
       t.NoteConsolidatore AS NoteConsolidatore

FROM pfd.[Notifiche] n
INNER JOIN pfd.Enti e ON e.InternalIstitutionId = n.internal_organization_id
INNER JOIN pfd.Contratti c ON e.InternalIstitutionId = c.internalistitutionid
LEFT JOIN pfd.Notifiche_CodiceOggetto g ON n.event_id = g.event_id
LEFT JOIN pfw.Contestazioni t ON t.FkIdNotifica = n.event_id
INNER JOIN pfw.FlagContestazione f ON f.IdFlagContestazione = ISNULL(t.FkIdFlagContestazione, 1)
LEFT JOIN pfw.TipoContestazione a ON a.IdTipoContestazione = t.FkIdTipoContestazione
";

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " ORDER BY n.year, n.month";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }

    private static string _sqlAnni = @"
SELECT  
      distinct [year]
   FROM [pfd].[NotificheCount]
";

    private static string _sqlMesi = @"
SELECT  
      distinct [month]
   FROM [pfd].[NotificheCount] 
";

    public static string OrderByYear()
    {
        return " ORDER BY year desc";
    }
    public static string OrderByMonth()
    {
        return " ORDER BY month desc";
    }
    public static string SelectAnni()
    {
        return _sqlAnni;
    }
 
    public static string SelectMesi()
    {
        return _sqlMesi;
    }

    public static string SelectRichiesteNotificheCount()
    {
        return @"SELECT  count(*) FROM [pfw].[ReportNotifiche] WHERE letto = 0 AND internal_organization_id=@idEnte AND stato IN (0,1)";
    }

    private static string _checkDownloadNotifiche = @"
SELECT
    OverallMaxDate.MaxDate AS DataMassimaContestazione,
    CASE
        WHEN FilteredByGivenDate.MaxDate IS NOT NULL THEN 1
        ELSE 0
    END AS NotificheDaScaricare,
    CASE
        WHEN OverallMaxDate.MaxDate IS NULL THEN 0
        ELSE 1
    END AS CiSonoContestazioni
FROM
    ( -- Subquery 1: Get the absolute overall MAX date for the given period (ignores @date)
        SELECT
            MAX(CalculatedDates.date_val) AS MaxDate
        FROM
            [pfw].[Contestazioni] c
        INNER JOIN
            [pfd].[Notifiche] n ON c.[FkIdNotifica] = n.[event_id]
        CROSS APPLY (VALUES
            (c.[DataInserimentoEnte]),
            (c.[DataModificaEnte]),
            (c.[DataInserimentoSend]),
            (c.[DataModificaSend]),
            (c.[DataInserimentoRecapitista]),
            (c.[DataModificaRecapitista]),
            (c.[DataInserimentoConsolidatore]),
            (c.[DataModificaConsolidatore]),
            (c.[DataChiusura])
        ) AS CalculatedDates(date_val)
        WHERE
            n.[internal_organization_id] = @idEnte
            AND c.[Anno] = @anno
            AND c.[Mese] = @mese
            AND CalculatedDates.date_val IS NOT NULL
    ) AS OverallMaxDate
CROSS JOIN
    ( -- Subquery 2: Get the MAX date specifically for entries >= @date (determines 'new' activity)
        SELECT
            MAX(CalculatedDates.date_val) AS MaxDate
        FROM
            [pfw].[Contestazioni] c
        INNER JOIN
            [pfd].[Notifiche] n ON c.[FkIdNotifica] = n.[event_id]
        CROSS APPLY (VALUES
            (c.[DataInserimentoEnte]),
            (c.[DataModificaEnte]),
            (c.[DataInserimentoSend]),
            (c.[DataModificaSend]),
            (c.[DataInserimentoRecapitista]),
            (c.[DataModificaRecapitista]),
            (c.[DataInserimentoConsolidatore]),
            (c.[DataModificaConsolidatore]),
            (c.[DataChiusura])
        ) AS CalculatedDates(date_val)
        WHERE
            n.[internal_organization_id] = @idEnte
            AND c.[Anno] = @anno
            AND c.[Mese] = @mese
            AND CalculatedDates.date_val IS NOT NULL
            AND CalculatedDates.date_val >  @date  
    ) AS FilteredByGivenDate;
";

    public static string CheckDownloadNotifiche()
    {
               return _checkDownloadNotifiche;
    }
}