using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Righe dei documenti contabili APP IO, non raggruppate, per il download "Documenti Contabili".
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportQueryGetByRicercaExcel(IAuthenticationInfo authenticationInfo)
    : IRequest<IEnumerable<AppIoFinancialReportRigaDto>>, IAppIoFinancialReportFiltro
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
