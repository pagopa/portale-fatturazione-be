using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Documenti emessi APP IO per la griglia: righe raggruppate per contratto e trimestre.
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportQueryGetByRicerca(IAuthenticationInfo authenticationInfo)
    : IRequest<AppIoFinancialReportListDto>, IAppIoFinancialReportFiltro
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <inheritdoc/>
    public string[]? InstitutionIds { get; set; }

    /// <inheritdoc/>
    public string[]? CodiciFiscali { get; set; }

    /// <inheritdoc/>
    public string[]? YearQuarter { get; set; }

    /// <inheritdoc/>
    public string? Year { get; set; }
}
