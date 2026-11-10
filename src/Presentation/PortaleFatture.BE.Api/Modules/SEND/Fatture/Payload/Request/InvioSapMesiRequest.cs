namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>Body di <c>POST api/fatture/invio-sap/mesi</c>: l'anno scelto nel filtro, obbligatorio.</summary>
public class InvioSapMesiRequest
{
    public int? Anno { get; set; }
}
