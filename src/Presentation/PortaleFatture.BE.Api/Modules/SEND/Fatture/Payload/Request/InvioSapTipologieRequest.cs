namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Body di <c>POST api/fatture/invio-sap/tipologie</c>: l'anno scelto nel filtro, obbligatorio, e i mesi scelti,
/// facoltativi. Senza mesi valgono tutti quelli dell'anno.
/// </summary>
public class InvioSapTipologieRequest
{
    public int? Anno { get; set; }
    public List<int>? Mesi { get; set; }
}
