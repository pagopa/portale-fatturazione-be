namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Body di <c>PUT api/fatture/invio-sap/genera</c> (D7). Senza <c>idFatture</c>: primo invio dell'intero rigo
/// <c>DA_INVIARE</c>, senza storno. Con <c>idFatture</c>: re-invio delle fatture scelte nel dettaglio di un rigo
/// <c>INVIATE</c>, con storno obbligatorio. <c>IdFatture</c> e' <c>long</c> come <c>pfd.FattureTestata.IdFattura</c>.
/// Con <c>AllSelected = true</c> ("seleziona tutto" del dettaglio paginato): re-invio di tutte le fatture inviate del
/// rigo, tranne <c>IdFattureEscluse</c>; <c>IdFatture</c> viene ignorato. Con <c>AllSelected</c> false o assente
/// vale <c>IdFatture</c> e <c>IdFattureEscluse</c> viene ignorato.
/// </summary>
public class InvioSapGeneraRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public bool? AllSelected { get; set; }
    public List<long>? IdFatture { get; set; }
    public List<long>? IdFattureEscluse { get; set; }
    public string? Nota { get; set; }
    public string? NumeroDocumentoStorno { get; set; }
}
