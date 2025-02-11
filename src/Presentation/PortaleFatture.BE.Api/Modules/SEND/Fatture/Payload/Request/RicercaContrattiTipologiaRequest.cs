using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public sealed class RicercaContrattiTipologiaRequest
{
    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }

    public int? TipologiaContratto { get; set; }  
}
