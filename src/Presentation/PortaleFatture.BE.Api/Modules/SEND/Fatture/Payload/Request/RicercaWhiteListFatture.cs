using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class RicercaWhiteListFatture
{
    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }

    public int? TipologiaContratto { get; set; } 
    public string? TipologiaFattura { get; set; }

    public int Anno { get; set; }

    private int[]? _mesi;
    public int[]? Mesi
    {
        get { return _mesi; }
        set { _mesi = value!.IsNullNotAny() ? null : value; }
    } 
} 