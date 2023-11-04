using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;

public sealed class DatiCommessaContattoDeleteCommand : IRequest<DatiCommessaContatto>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long IdDatiCommessa { get; set; } 
} 