using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

/// <summary>
/// Ricerca degli enti APP IO per nome, per popolare il filtro "Nome Ente" (anagrafica e documenti emessi).
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoContrattiQueryGetByName(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<AppIoContrattoNome>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <summary>
    /// Testo da cercare nel nome dell'ente (ricerca per sottostringa); null = nessun filtro sul nome.
    /// </summary>
    public string? Name { get; set; }
}
