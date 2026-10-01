using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

/// <summary>
/// Trimestri distinti in cui sono presenti contratti APP IO, per i filtri "Anno" e "Trimestre".
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoContrattiQuartersQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <summary>
    /// Anno di cui restituire i trimestri; null o vuoto = tutti i trimestri.
    /// </summary>
    public string? Year { get; set; }
}
