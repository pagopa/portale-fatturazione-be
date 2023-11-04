using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries;

public class TipoContrattoQueryGetAll : IRequest<IEnumerable<TipoContratto>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
} 