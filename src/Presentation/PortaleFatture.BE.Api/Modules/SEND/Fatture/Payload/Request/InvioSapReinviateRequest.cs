namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>Body di <c>POST api/fatture/invio-sap/download/reinviate</c>. Per un rigo il FE passa un mese e una tipologia.</summary>
public class InvioSapReinviateRequest
{
    public int? Anno { get; set; }
    public List<int>? Mesi { get; set; }
    public List<string>? TipologieFattura { get; set; }
}
