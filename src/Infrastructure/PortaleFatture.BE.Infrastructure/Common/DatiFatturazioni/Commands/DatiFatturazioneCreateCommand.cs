using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;

public sealed class DatiFatturazioneCreateCommand(IAuthenticationInfo authenticationInfo) : IRequest<DatiFatturazione>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Cup { get; set; }
    public bool NotaLegale { get; set; }
    public string? CodCommessa { get; set; }
    public DateTime?   DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }  
    public string? IdDocumento { get; set; }
    public string? Map { get; set; } 
    public string? TipoCommessa { get; set; }  
    public string? Pec { get; set; }
    public DateTime? DataCreazione { get; set; } 
    public List<DatiFatturazioneContattoCreateCommand>? Contatti { get; set; }
}