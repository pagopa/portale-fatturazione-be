namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Dettaglio di un rigo espanso (<c>POST api/fatture/invio-sap/dettaglio</c>). I quattro campi del rigo sono
/// obbligatori; il rigo deve essere <c>INVIATE</c> o <c>IN_ELABORAZIONE</c>: per <c>DA_INVIARE</c> il dettaglio
/// non esiste (C1) e la risposta e' 400. Filtri e paginazione sono opzionali (page e pageSize insieme).
/// </summary>
public class InvioSapDettaglioRequest
{
    public int? AnnoRiferimento { get; set; }
    public int? MeseRiferimento { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? StatoInvio { get; set; }
    public string? RagioneSociale { get; set; }
    public string? NumeroFattura { get; set; }
    public DateTime? DataFattura { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
