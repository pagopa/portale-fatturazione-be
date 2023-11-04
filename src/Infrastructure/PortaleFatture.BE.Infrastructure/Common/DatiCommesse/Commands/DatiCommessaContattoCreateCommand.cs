using System.Runtime.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;

public sealed class DatiCommessaContattoCreateCommand : IRequest<DatiCommessaContatto>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long IdDatiCommessa { get; set; }
    public string? Email { get; set; }
    public int Tipo { get; set; }
}