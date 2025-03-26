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
}