using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class RelTestataRicercaRequestPagoPA
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdContratto { get; set; } 
    public byte? Caricata { get; set; }

    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }
}