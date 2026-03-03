using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class FatturaRiepilogoRicercaRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }

    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }
    // ! TODO: da valutare se è necessario o meno, al momento non è utilizzato e non è presente nella query di ricerca del riepilogo
    // private string[]? _tipologiaContratto;
    // public string[]? TipologiaContratto
    // {
    //     get { return _tipologiaContratto; }
    //     set { _tipologiaContratto = value!.IsNullNotAny() ? null : value; }
    // }
}