using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Orchestratore.Payload;

public sealed class OrchestratoreByDateRequest
{
    public DateTime? Init { get; set; }
    public DateTime? End { get; set; }  

    private short[]? _stati;
    public short[]? Stati
    {
        get { return _stati; }
        set { _stati = value!.IsNullNotAny() ? null : value; }
    } 
    public int? Ordinamento { get; set; }

    private string[]? _tipologie;
    public string[]? Tipologie
    {
        get { return _tipologie; }
        set { _tipologie = value!.IsNullNotAny() ? null : value; }
    }

    private string[]? _fasi;
    public string[]? Fasi
    {
        get { return _fasi; }
        set { _fasi = value!.IsNullNotAny() ? null : value; }
    }
}