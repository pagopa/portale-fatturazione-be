using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public class RECCONNotificaDto
{
    [Column("internal_organization_id")]
    [HeaderAttributeRECCON(caption: "internal_organization_id", Order = 16)]
    public string? IdEnte { get; set; }

    [Column("institutionType")]
    [HeaderAttributeRECCON(caption: "institutionType", Order = 28)]
    public string? Profilo { get; set; }

    [Column("contract_id")]
    [HeaderAttributeRECCON(caption: "contract_id", Order = 1)]
    public string? IdContratto { get; set; }

    [Column("zip_code")]
    [HeaderAttributeRECCON(caption: "zip_code", Order = 4)]
    public string? CAP { get; set; }

    [Column("foreign_state")]
    [HeaderAttributeRECCON(caption: "foreign_state", Order = 5)]
    public string? StatoEstero { get; set; }

    [Column("number_of_pages")]
    [HeaderAttributeRECCON(caption: "number_of_pages", Order = 6)]
    public string? NumberOfPages { get; set; }

    [Column("g_envelope_weight")]
    [HeaderAttributeRECCON(caption: "g_envelope_weight", Order = 7)]
    public string? GEnvelopeWeight { get; set; }

    [Column("cost_eurocent")]
    [HeaderAttributeRECCON(caption: "cost_eurocent", Order = 8)]
    public string? CostEuroInCentesimi { get; set; }

    [Column("timeline_category")]
    [HeaderAttributeRECCON(caption: "timeline_category", Order = 9)]
    public string? TimelineCategory { get; set; }

    [Column("contestazione")]
    [Header(caption: "contestazione", Order = 18)]
    public string? Contestazione { get; set; }

    [Column("idContestazione")]
    public short StatoContestazione { get; set; }

    [Column("notificationtype")]
    [HeaderAttributeRECCON(caption: "paper_product_type", Order = 10)]
    public string? TipoNotifica { get; set; }

    [Column("event_id")]
    [HeaderAttributeRECCON(caption: "event_id", Order = 11)]
    public string? IdNotifica { get; set; }

    [Column("iun")]
    [HeaderAttributeRECCON(caption: "iun", Order = 12)]
    public string? IUN { get; set; }

    [Column("notification_sent_at")]
    [HeaderAttributeRECCON(caption: "notification_sent_at", Order = 15)]
    public string? DataInvio { get; set; }

    [Column("event_timestamp")]
    [HeaderAttributeRECCON(caption: "event_timestamp", Order = 17)]
    public string? Data { get; set; }

    [Column("recipient_index")]
    [HeaderAttributeRECCON(caption: "recipient_index", Order = 18)]
    public string? RecipientIndex { get; set; }

    [Column("recipient_type")]
    [HeaderAttributeRECCON(caption: "recipient_type", Order = 19)]
    public string? RecipientType { get; set; }

    [Column("recipient_id")]
    [HeaderAttributeRECCON(caption: "recipient_id", Order = 20)]
    public string? RecipientId { get; set; }

    [Column("year")]
    [HeaderAttributeRECCON(caption: "year", Order = 21)]
    public string? Anno { get; set; }

    [Column("month")]
    [HeaderAttributeRECCON(caption: "month", Order = 22)]
    public string? Mese { get; set; }

    [Column("daily")]
    [HeaderAttributeRECCON(caption: "daily", Order = 23)]
    public string? AnnoMeseGiorno { get; set; }

    [Column("item_code")]
    [HeaderAttributeRECCON(caption: "item_code", Order = 24)]
    public string? ItemCode { get; set; }

    [Column("notification_request_id")]
    [HeaderAttributeRECCON(caption: "notification_request_id", Order = 25)]
    public string? NotificationRequestId { get; set; }

    [Column("recipient_tax_id")]
    [HeaderAttributeRECCON(caption: "recipient_tax_id", Order = 26)]
    public string? RecipientTaxId { get; set; }

    [Column("Fatturabile")]
    public bool? Fatturata { get; set; }

    [Column("onere")]
    public string? Onere { get; set; }

    [Column("NoteEnte")]
    [HeaderAttributeRECCON(caption: "Note Ente", Order = 30)]
    public string? NoteEnte { get; set; }

    [Column("RispostaEnte")]
    [HeaderAttributeRECCON(caption: "Risposta Ente", Order = 31)]
    public string? RispostaEnte { get; set; }

    [Column("NoteSend")]
    [HeaderAttributeRECCON(caption: "Risposta Send", Order = 32)]
    public string? NoteSend { get; set; }

    [Column("NoteRecapitista")]
    [HeaderAttributeRECCON(caption: "Risposta Recapitista", Order = 33)]
    public string? NoteRecapitista { get; set; }

    [Column("NoteConsolidatore")]
    [HeaderAttributeRECCON(caption: "Risposta Consolidatore", Order = 35)]
    public string? NoteConsolidatore { get; set; }

    [Column("TipoContestazione")]
    [HeaderAttributeRECCON(caption: "Tipo Contestazione", Order = 40)]
    public string? TipoContestazione { get; set; }

    [Column("TipologiaFattura")]
    [HeaderAttributeRECCON(caption: "Tipologia Fattura", Order = 41)]
    public string? TipologiaFattura { get; set; }
}