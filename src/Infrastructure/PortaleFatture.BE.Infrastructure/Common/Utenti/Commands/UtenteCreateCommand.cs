using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Utenti;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.Commands;

public class UtenteCreateCommand : IRequest<Utente?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public UtenteCreateCommand(IAuthenticationInfo? authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    }
    public DateTime? DataPrimo { get; set; }
    public DateTime? DataUltimo { get; set; }
} 