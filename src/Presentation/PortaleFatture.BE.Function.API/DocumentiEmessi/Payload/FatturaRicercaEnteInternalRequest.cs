using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.DocumentiEmessi.Payload;

public class FatturaRicercaEnteInternalRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }

    private string[]? _tipologiaFattura;
    public string[]? TipologiaFattura
    {
        get { return _tipologiaFattura; }
        set { _tipologiaFattura = value!.IsNullNotAny() ? null : value; }
    }
    public Session? Session { get; set; }
}