using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;

public class SimpleNotificaDto
{
    [Column("internal_organization_id")]
    public string? IdEnte { get; set; }

    [Column("description")]
    public string? RagioneSociale { get; set; }

    [Column("institutionType")]
    public string? Profilo { get; set; }

    [Column("contract_id")]
    [HeaderAttribute(caption: "contract_id", Order = 1)]
    public string? IdContratto { get; set; }

    [Column("tax_code")]
    [HeaderAttribute(caption: "tax_code", Order = 2)]
    public string? CodiceFiscale { get; set; }

    [Column("vat_number")]
    [HeaderAttribute(caption: "vat_number", Order = 3)]
    public string? PIva { get; set; }

    [Column("zip_code")]
    [HeaderAttribute(caption: "zip_code", Order = 4)]
    public string? CAP { get; set; }

    [Column("foreign_state")]
    [HeaderAttribute(caption: "foreign_state", Order = 5)]
    public string? StatoEstero { get; set; }

    [Column("number_of_pages")]
    public string? NumberOfPages { get; set; }

    [Column("g_envelope_weight")]
    public string? GEnvelopeWeight { get; set; }

    [Column("cost_eurocent")]
    [HeaderAttribute(caption: "cost_eurocent", Order = 6)]
    public string? CostEuroInCentesimi { get; set; } 

    [Column("timeline_category")]
    [HeaderAttribute(caption: "timeline_category", Order = 7)]
    public string? TimelineCategory { get; set; }

    [Column("contestazione")]
    [HeaderAttribute(caption: "contestazione", Order = 18)]
    public string? Contestazione { get; set; }

    [Column("idContestazione")]
    public short StatoContestazione { get; set; }

    //[Column("tipoContestazione")]
    //public string? TipoContestazione { get; set; }

    //private string? _tipoNotifica;

    //[Column("paper_product_type")]
    //[HeaderAttribute(caption: "paper_product_type", Order = 8)]
    //public string? TipoNotifica
    //{
    //    get => _tipoNotifica.Map();
    //    set { _tipoNotifica = value; }
    //}

    [Column("notificationtype")]
    [HeaderAttribute(caption: "paper_product_type", Order = 8)]
    public string? TipoNotifica { get; set; } 

    [Column("event_id")]
    [HeaderAttribute(caption: "event_id", Order = 9)]
    public string? IdNotifica { get; set; }

    [Column("iun")]
    [HeaderAttribute(caption: "iun", Order = 10)]
    public string? IUN { get; set; }

    [Column("consolidatore")]
    [HeaderAttribute(caption: "consolidatore", Order = 11)]
    public string? Consolidatore { get; set; }

    [Column("recapitista")]
    [HeaderAttribute(caption: "recapitista", Order = 12)]
    public string? Recapitista { get; set; }

    [Column("notification_sent_at")]
    [HeaderAttribute(caption: "notification_sent_at", Order = 13)]
    public string? DataInvio { get; set; }

    [Column("event_timestamp")]
    [HeaderAttribute(caption: "event_timestamp", Order = 14)]
    public string? Data { get; set; }

    [Column("recipient_index")]
    public string? RecipientIndex { get; set; }

    [Column("recipient_type")]
    public int RecipientType { get; set; }

    [Column("recipient_id")]
    public int RecipientId { get; set; }

    [Column("year")]
    [HeaderAttribute(caption: "year", Order = 15)]
    public string? Anno { get; set; }

    [Column("month")]
    [HeaderAttribute(caption: "month", Order = 16)]
    public string? Mese { get; set; }

    [Column("daily")]
    [HeaderAttribute(caption: "daily", Order = 17)]
    public string? AnnoMeseGiorno { get; set; }

    [Column("item_code")]
    public string? ItemCode { get; set; }

    [Column("notification_request_id")]
    public string? NotificationRequestId { get; set; }

    [Column("recipient_tax_id")]
    public string? RecipientTaxId { get; set; }

    [Column("fatturata")]
    public bool Fatturata { get; set; }
} 