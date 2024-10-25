using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

public sealed class ProdottoQueryGetAll : IRequest<IEnumerable<Prodotto>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}