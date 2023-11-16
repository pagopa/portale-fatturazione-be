using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;

public sealed class DatiFatturazioneUpdateCommand : IRequest<DatiFatturazione>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long Id { get; set; }
    public string? Cup { get; set; }
    public string? Cig { get; set; }
    public string? CodCommessa { get; set; }
    public DateTimeOffset DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }
    public string? IdEnte { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? TipoCommessa { get; set; }
    public string? Prodotto { get; set; }
    public string? Pec { get; set; }
    public DateTimeOffset? DataModifica { get; set; }
    public List<DatiFatturazioneContattoCreateCommand>? Contatti { get; set; }
}