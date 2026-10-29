namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Body di <c>PUT api/fatture/invio-sap/genera</c> (RF-8, RF-16). <c>IdFatture</c> e' <c>long</c>:
/// <c>pfd.FattureTestata.IdFattura</c> e' BIGINT (stessa correzione gia' fatta su Gestione Fatture).
/// </summary>
public class InvioSapGeneraRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public List<long>? IdFatture { get; set; }
    public string? Nota { get; set; }
    /// <summary>Obbligatorio solo se la selezione contiene fatture gia' inviate.</summary>
    public string? NumeroDocumentoStorno { get; set; }
}
