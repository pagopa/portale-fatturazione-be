namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Dettaglio del rigo espanso (<c>POST api/fatture/invio-sap/dettaglio</c>): le fatture dello stesso
/// Stato/Tipologia/Anno/Mese (RF-3, RF-6). I quattro campi del gruppo sono obbligatori; i filtri di
/// dettaglio (ragione sociale, numero fattura, data fattura) e la paginazione sono opzionali.
/// </summary>
public class InvioSapDettaglioRequest
{
    public int? AnnoRiferimento { get; set; }
    public int? MeseRiferimento { get; set; }
    public string? TipologiaFattura { get; set; }
    /// <summary>Stato del rigo espanso: <c>DA_INVIARE | IN_ELABORAZIONE | INVIATA</c>.</summary>
    public string? StatoInvio { get; set; }
    public string? RagioneSociale { get; set; }
    public string? NumeroFattura { get; set; }
    public DateTime? DataFattura { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
