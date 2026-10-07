using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Contenuto del download "Financial Report" APP IO: righe del financial report e posizioni.
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportQueryGetDettaglioExcel(IAuthenticationInfo authenticationInfo)
    : IRequest<AppIoFinancialReportDettaglioDto>, IAppIoFinancialReportFiltro
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <inheritdoc/>
    public string[]? ContractIds { get; set; }

    /// <inheritdoc/>
    public string[]? YearQuarter { get; set; }

    /// <inheritdoc/>
    public string? Year { get; set; }
}
