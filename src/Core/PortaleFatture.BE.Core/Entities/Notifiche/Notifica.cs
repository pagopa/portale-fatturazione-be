using System.ComponentModel.DataAnnotations.Schema; 

namespace PortaleFatture.BE.Core.Entities.Notifiche;
public class Notifica
{
    [Column("internal_organization_id")]
    public string? IdEnte { get; set; }

    [Column("description")]
    public string? RagioneSociale { get; set; }

    [Column("institutionType")]
    public string? Profilo { get; set; }

    [Column("contract_id")]
    public string? IdContratto { get; set; }

    [Column("tax_code")]
    public string? CodiceFiscale { get; set; }

    [Column("vat_number")]
    public string? PIva { get; set; }

    [Column("zip_code")]
    public string? CAP { get; set; }

    [Column("foreign_state")]
    public string? StatoEstero { get; set; }

    [Column("number_of_pages")]
    public string? NumberOfPages { get; set; }

    [Column("g_envelope_weight")]
    public string? GEnvelopeWeight { get; set; }

    [Column("cost_eurocent")]
    public string? CostEuroInCentesimi { get; set; }

    [Column("timeline_category")]
    public string? TimelineCategory { get; set; }

    [Column("contestazione")]
    public string? Contestazione { get; set; }

    [Column("idContestazione")] 
    public short StatoContestazione { get; set; }

    [Column("tipoContestazione")]
    public string? TipoContestazione { get; set; }

    private string? _tipoNotifica;

    [Column("paper_product_type")]
    public string? TipoNotifica
    {
        get => _tipoNotifica.Map();
        set { _tipoNotifica = value; }
    }

    [Column("event_id")]
    public string? IdNotifica { get; set; }

    [Column("iun")]
    public string? IUN { get; set; }

    [Column("notification_sent_at")]
    public string? DataInvio { get; set; }

    [Column("event_timestamp")]
    public string? Data { get; set; }

    [Column("recipient_index")]
    public string? RecipientIndex { get; set; }

    [Column("recipient_type")]
    public string? RecipientType { get; set; }

    [Column("recipient_id")]
    public string? RecipientId { get; set; }

    [Column("year")]
    public string? Anno { get; set; }

    [Column("month")]
    public string? Mese { get; set; }

    [Column("daily")]
    public string? AnnoMeseGiorno { get; set; }

    [Column("item_code")]
    public string? ItemCode { get; set; }

    [Column("notification_request_id")]
    public string? NotificationRequestId { get; set; }

    [Column("recipient_tax_id")]
    public string? RecipientTaxId { get; set; }

    [Column("Fatturabile")]
    public bool? Fatturata { get; set; }

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [Column("Recapitista")]
    public string? Recapitista { get; set; }

    [Column("Consolidatore")]
    public string? Consolidatore { get; set; }
}