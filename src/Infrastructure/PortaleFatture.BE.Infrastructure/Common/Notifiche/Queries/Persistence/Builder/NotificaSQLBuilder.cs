namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence.Builder;

internal static class NotificaSQLBuilder
{
    private static string _sqlCount = @"
SELECT Count(internal_organization_id) 
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
       [event_id] AS IdNotifica,
       [iun] AS Iun,
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
       e.description AS RagioneSociale,
       e.institutionType AS Profilo,
       c.product AS Prodotto,
       f.FlagContestazione AS Contestazione,
       f.IdFlagContestazione AS StatoContestazione,
       a.TipoContestazione AS TipoContestazione,
       0 as Fatturata,
       t.onere AS Onere
FROM pfd.[Notifiche] n
INNER JOIN pfd.Enti e ON e.InternalIstitutionId = n.internal_organization_id
INNER JOIN pfd.Contratti c ON e.InternalIstitutionId = c.internalistitutionid
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
        return " ORDER BY n.year DESC, n.month";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }
} 