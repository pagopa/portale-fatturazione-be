using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

/// <summary>
/// Ricerca dei contratti APP IO per la griglia e per il download Excel.
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoContrattiQueryGetByRicerca(IAuthenticationInfo authenticationInfo) : IRequest<AppIoContrattiListDto>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <summary>
    /// Numero di pagina (da 1). La paginazione si applica solo se sono valorizzati sia Page sia Size.
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// Dimensione della pagina. La paginazione si applica solo se sono valorizzati sia Page sia Size.
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// Enti da cercare (internalistitutionid); null o vuoto = nessun filtro.
    /// </summary>
    public string[]? InstitutionIds { get; set; }

    /// <summary>
    /// Tipi ente da cercare (es. PA, GSP); null o vuoto = nessun filtro.
    /// </summary>
    public string[]? TipiEnte { get; set; }
}
