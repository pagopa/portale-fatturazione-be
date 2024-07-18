using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Utenti;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.Queries;

public class UtenteQueryGetById : IRequest<Utente?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public UtenteQueryGetById(IAuthenticationInfo? authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    }
    public DateTime DataPrimo { get; set; }
    public DateTime DataUltimo { get; set; }
} 