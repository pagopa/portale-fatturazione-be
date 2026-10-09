using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

/// <summary>
/// Ricerca dei contratti APP IO per nome dell'ente, per popolare il filtro "Nome Ente".
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

    /// <summary>
    /// Trimestri in cui cercare, nel formato 'AAAA_T'; prevalgono su Year. Null o vuoto = si usa Year.
    /// </summary>
    public string[]? YearQuarter { get; set; }

    /// <summary>
    /// Anno ('AAAA'), usato solo senza trimestri: tutti i trimestri dell'anno. Senza trimestri né
    /// anno = tutti i trimestri.
    /// </summary>
    public string? Year { get; set; }
}
