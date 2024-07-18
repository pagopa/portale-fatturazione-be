using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Request;
public sealed class EnteRicercaModuloCommessaByDescrizioneRequest
{
    public int? Anno { get; set; } 
    public int? Mese { get; set; }
    public string? Prodotto { get; set; }

    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }
}