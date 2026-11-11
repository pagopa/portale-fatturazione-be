using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

/// <summary>
/// Tipi ente distinti dei contratti APP IO, per il filtro "Tipo Ente".
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoContrattiTipiEnteQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}
