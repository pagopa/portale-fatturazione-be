using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.CommandHandlers;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;
 
public sealed class DatiCommessaUpdateCommand : IRequest<DatiCommessa>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long Id { get; set; }
    public string? Cup { get; set; }
    public string? Cig { get; set; }
    public string? CodCommessa { get; set; }
    public DateTimeOffset DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }
    public long? IdTipoContratto { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? FlagOrdineContratto { get; set; } 
    public DateTimeOffset DataModifica { get; set; } 
    public List<DatiCommessaContattoCreateCommand>? Contatti { get; set; }
}