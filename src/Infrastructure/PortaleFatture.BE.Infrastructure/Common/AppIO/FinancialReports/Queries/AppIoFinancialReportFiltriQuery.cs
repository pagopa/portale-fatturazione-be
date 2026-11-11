using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Anni distinti dei documenti emessi APP IO, da [be].[vwAppioFinalReportFiltroAnno] (filtro "Anno").
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportYearsQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<int>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}

/// <summary>
/// Codici fiscali distinti degli enti APP IO, da [be].[vwAppioFinalReportFiltroCodFiscaleEnte]
/// (filtro "Codice Fiscale").
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportCodiciFiscaliQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}
