using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class FatturaRicercaEnteRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }  

    private string[]? _tipologiaFattura;
    public string[]? TipologiaFattura
    {
        get { return _tipologiaFattura; }
        set { _tipologiaFattura = value!.IsNullNotAny() ? null : value; }
    }
}