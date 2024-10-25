namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

public static class RelRigheSQLBuilder
{
    private static string _sql = @"
    SELECT [contract_id] AS IdContratto,
           [tax_code] AS CodiceFiscale,
           [vat_number] AS PIva,
           [zip_code] AS CAP,
           [foreign_state] AS StatoEstero,
           [number_of_pages] AS NumberOfPages,
           [g_envelope_weight] AS GEnvelopeWeight,
           [cost] AS Cost,
           [timeline_category] AS TimelineCategory,
           [notificationtype] AS TipoNotifica,
           [event_id] AS IdNotifica,
           [iun] AS Iun,
           [notification_sent_at] AS DataInvio ,
           r.[internal_organization_id] AS IdEnte ,
           [event_timestamp] AS DATA ,
           [recipient_index] AS RecipientIndex,
           [recipient_type] AS RecipientType,
           [recipient_id] AS RecipientId,
           r.[year] AS anno,
           r.[month] AS mese,
           r.[daily] AS AnnoMeseGiorno,
           [item_code] AS ItemCode,
           [notification_request_id] AS NotificationRequestId,
           [recipient_tax_id] AS RecipientTaxId,
           [Recapitista] AS Recapitista,
           e.description AS RagioneSociale,  
		   CAST(
             CASE
                  WHEN  [TipologiaFattura] = 'ASSEVERAZIONE'
                     THEN 'Notifica di ente aderente al bando PNRR in cui è prevista la fase di asseverazione'
                  ELSE [TipologiaFattura] 
             END AS nvarchar(300)) as TipologiaFattura 
    FROM pfd.RelRighe r
    INNER JOIN pfd.Enti e ON e.InternalIstitutionId = r.internal_organization_id
    INNER JOIN pfd.Contratti c ON e.InternalIstitutionId = c.internalistitutionid   
    ";

    public static string SelectAll()
    {
        return _sql;
    }

}