using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Tipologie;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public class StatoCommessaQueryGetByDefault : IRequest<StatoCommessa?>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
} 