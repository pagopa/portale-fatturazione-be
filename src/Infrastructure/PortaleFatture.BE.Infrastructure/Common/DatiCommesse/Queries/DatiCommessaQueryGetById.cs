using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries;

public class DatiCommessaQueryGetById : IRequest<DatiCommessa>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long Id { get; set; }
} 