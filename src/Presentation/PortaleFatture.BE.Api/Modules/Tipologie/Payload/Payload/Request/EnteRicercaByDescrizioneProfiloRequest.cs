using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.Tipologie.Payload.Payload.Request;

public sealed class EnteRicercaByDescrizioneProfiloRequest
{

    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }
    public string? Prodotto { get; set; }
    public string? Profilo { get; set; }
}

public sealed class EnteRicercaByRequest
{
    public string? Descrizione { get; set; } 
    public string? Prodotto { get; set; }
    public string? Profilo { get; set; }
}