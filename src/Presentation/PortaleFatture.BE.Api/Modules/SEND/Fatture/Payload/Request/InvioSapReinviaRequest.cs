namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Body di <c>PUT api/fatture/invio-sap/reinvia</c>: re-invio di fatture gia' inviate di un solo rigo (anno, mese,
/// tipologia). Ogni fattura porta la propria nota e il proprio documento di storno, tutti obbligatori.
/// Al massimo 1000 fatture per chiamata, come nel Genera.
/// </summary>
public class InvioSapReinviaRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public List<InvioSapReinviaFatturaRequest?>? Fatture { get; set; }
}

/// <summary>Una fattura da re-inviare. <c>IdFattura</c> e' <c>long</c> come <c>pfd.FattureTestata.IdFattura</c>.</summary>
public class InvioSapReinviaFatturaRequest
{
    public long? IdFattura { get; set; }
    public string? Nota { get; set; }
    public string? NumeroDocumentoStorno { get; set; }
}
