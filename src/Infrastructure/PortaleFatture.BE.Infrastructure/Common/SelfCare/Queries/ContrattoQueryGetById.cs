using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;

public class ContrattoQueryGetById : IRequest<Contratto?>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; } 
    public string? IdEnte { get; set; }
} 