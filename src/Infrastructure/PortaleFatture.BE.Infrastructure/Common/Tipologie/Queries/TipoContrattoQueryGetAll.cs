using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Tipologie;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public class TipoContrattoQueryGetAll : IRequest<IEnumerable<TipoContratto>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}