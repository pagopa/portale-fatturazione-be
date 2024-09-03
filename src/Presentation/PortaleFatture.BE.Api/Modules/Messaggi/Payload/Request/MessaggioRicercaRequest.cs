using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.Messaggi.Payload.Request;

public class MessaggioRicercaRequest
{ 
    public int? Anno { get; set; }
    public int? Mese { get; set; }  
 
    private string[]? _tipologiaDocumento;
    public string[]? TipologiaDocumento
    {
        get { return _tipologiaDocumento; }
        set { _tipologiaDocumento = value!.IsNullNotAny() ? null : value; }
    }
    public bool? Letto { get; set; } 
} 