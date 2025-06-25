using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public class SimpleNotificaDto
{
    [Column("internal_organization_id")]
    [HeaderAttributev2(caption: "internal_organization_id", Order = 16)]
    public string? IdEnte { get; set; }

    [Column("description")]
    [HeaderAttributev2(caption: "description", Order = 27)]
    public string? RagioneSociale { get; set; }

    [Column("institutionType")]
    [HeaderAttributev2(caption: "institutionType", Order = 28)]
    public string? Profilo { get; set; }

    [Column("contract_id")]
    [Header(caption: "contract_id", Order = 1)]
    [HeaderAttributev2(caption: "contract_id", Order = 1)]
    public string? IdContratto { get; set; }

    [Column("tax_code")]
    [Header(caption: "tax_code", Order = 2)]
    [HeaderAttributev2(caption: "tax_code", Order = 2)]
    public string? CodiceFiscale { get; set; }

    [Column("vat_number")]
    [Header(caption: "vat_number", Order = 3)]
    [HeaderAttributev2(caption: "vat_number", Order = 3)]
    public string? PIva { get; set; }

    [Column("zip_code")]
    [Header(caption: "zip_code", Order = 4)]
    [HeaderAttributev2(caption: "zip_code", Order = 4)]
    public string? CAP { get; set; }

    [Column("foreign_state")]
    [Header(caption: "foreign_state", Order = 5)]
    [HeaderAttributev2(caption: "foreign_state", Order = 5)]
    public string? StatoEstero { get; set; }

    [Column("number_of_pages")]
    [HeaderAttributev2(caption: "number_of_pages", Order = 6)]
    public string? NumberOfPages { get; set; }

    [Column("g_envelope_weight")]
    [HeaderAttributev2(caption: "g_envelope_weight", Order = 7)]
    public string? GEnvelopeWeight { get; set; }

    [Column("cost_eurocent")]
    [Header(caption: "cost_eurocent", Order = 6)]
    [HeaderAttributev2(caption: "cost_eurocent", Order = 8)]
    public string? CostEuroInCentesimi { get; set; }

    [Column("timeline_category")]
    [Header(caption: "timeline_category", Order = 7)]
    [HeaderAttributev2(caption: "timeline_category", Order = 9)]
    public string? TimelineCategory { get; set; }

    [Column("contestazione")]
    [Header(caption: "contestazione", Order = 18)]
    public string? Contestazione { get; set; }

    [Column("idContestazione")]
    public short StatoContestazione { get; set; }

    [Column("notificationtype")]
    [Header(caption: "paper_product_type", Order = 8)]
    [HeaderAttributev2(caption: "paper_product_type", Order = 10)]
    public string? TipoNotifica { get; set; }


    [Column("notificationtype")]
    [Header(caption: "notification_type", Order = 8)]
    [HeaderAttributev2(caption: "notification_type", Order = 10)] 
    public string? NotificationType { get; set; }

    [Column("event_id")]
    [Header(caption: "event_id", Order = 9)]
    [HeaderAttributev2(caption: "event_id", Order = 11)]
    public string? IdNotifica { get; set; }

    [Column("iun")]
    [Header(caption: "iun", Order = 10)]
    [HeaderAttributev2(caption: "iun", Order = 12)]
    public string? IUN { get; set; }

    [Column("consolidatore")]
    [Header(caption: "consolidatore", Order = 11)]
    [HeaderAttributev2(caption: "consolidatore", Order = 13)]
    public string? Consolidatore { get; set; }

    [Column("recapitista")]
    [Header(caption: "recapitista", Order = 12)]
    [HeaderAttributev2(caption: "recapitista", Order = 14)]
    public string? Recapitista { get; set; }

    [Column("notification_sent_at")]
    [Header(caption: "notification_sent_at", Order = 13)]
    [HeaderAttributev2(caption: "notification_sent_at", Order = 15)]
    public string? DataInvio { get; set; }

    [Column("event_timestamp")]
    [Header(caption: "event_timestamp", Order = 14)]
    [HeaderAttributev2(caption: "event_timestamp", Order = 17)]
    public string? Data { get; set; }

    [Column("recipient_index")]
    [HeaderAttributev2(caption: "recipient_index", Order = 18)]
    public string? RecipientIndex { get; set; }

    [Column("recipient_type")]
    [HeaderAttributev2(caption: "recipient_type", Order = 19)]
    public string? RecipientType { get; set; }

    [Column("recipient_id")]
    [HeaderAttributev2(caption: "recipient_id", Order = 20)]
    public string? RecipientId { get; set; }

    [Column("year")]
    [Header(caption: "year", Order = 15)]
    [HeaderAttributev2(caption: "year", Order = 21)]
    public string? Anno { get; set; }

    [Column("month")]
    [Header(caption: "month", Order = 16)]
    [HeaderAttributev2(caption: "month", Order = 22)]
    public string? Mese { get; set; }

    [Column("daily")]
    [Header(caption: "daily", Order = 17)]
    [HeaderAttributev2(caption: "daily", Order = 23)]
    public string? AnnoMeseGiorno { get; set; }

    [Column("item_code")]
    [HeaderAttributev2(caption: "item_code", Order = 24)]
    public string? ItemCode { get; set; }

    [Column("notification_request_id")]
    [HeaderAttributev2(caption: "notification_request_id", Order = 25)]
    public string? NotificationRequestId { get; set; }

    [Column("recipient_tax_id")]
    [HeaderAttributev2(caption: "recipient_tax_id", Order = 26)]
    public string? RecipientTaxId { get; set; }

    [Column("Fatturabile")]
    public bool? Fatturata { get; set; }

    [Column("onere")]
    public string? Onere { get; set; }

    [Column("NoteEnte")]
    [Header(caption: "Note Ente", Order = 18)]
    [HeaderAttributev2(caption: "Note Ente", Order = 27)]
    public string? NoteEnte { get; set; }

    [Column("RispostaEnte")]
    [Header(caption: "Risposta Ente", Order = 19)]
    [HeaderAttributev2(caption: "Risposta Ente", Order = 28)]
    public string? RispostaEnte { get; set; }

    [Column("NoteSend")]
    [Header(caption: "Risposta Send", Order = 20)]
    [HeaderAttributev2(caption: "Risposta Send", Order = 29)]
    public string? NoteSend { get; set; }

    [Column("NoteRecapitista")]
    [Header(caption: "Risposta Recapitista", Order = 21)]
    [HeaderAttributev2(caption: "Risposta Recapitista", Order = 30)]
    public string? NoteRecapitista { get; set; }

    [Column("NoteConsolidatore")]
    [Header(caption: "Risposta Consolidatore", Order = 22)]
    [HeaderAttributev2(caption: "Risposta Consolidatore", Order = 31)]
    public string? NoteConsolidatore { get; set; }

    [Column("TipoContestazione")]
    [Header(caption: "Tipo Contestazione", Order = 23)]
    [HeaderAttributev2(caption: "Tipo Contestazione", Order = 32)]
    public string? TipoContestazione { get; set; }

    [Column("IdTipoContestazione")]
    [Header(caption: "Id Tipo Contestazione", Order = 24)]
    [HeaderAttributev2(caption: "Id Tipo Contestazione", Order = 33)] 
    public int? IdTipoContestazione { get; set; }

    [Column("TipologiaFattura")]
    [Header(caption: "Tipologia Fattura", Order = 25)]
    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 34)]
    public string? TipologiaFattura { get; set; }

    [Column("CodiceOggetto")]
    [Header(caption: "Codice Oggetto", Order = 26)]
    [HeaderAttributev2(caption: "Codice Oggetto", Order = 35)]
    public string? CodiceOggetto { get; set; }
}