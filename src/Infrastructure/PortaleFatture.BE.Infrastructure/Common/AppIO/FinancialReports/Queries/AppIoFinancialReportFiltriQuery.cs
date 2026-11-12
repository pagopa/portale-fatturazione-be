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
/// Codici fiscali distinti degli enti APP IO che hanno documenti emessi, da
/// [be].[vwAppioFinalReportFiltroCodFiscaleEnte] (filtro "Codice Fiscale").
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportCodiciFiscaliQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}

/// <summary>
/// Percorso sul blob storage del file del financial report APP IO di un trimestre, da
/// [be].[vwAppioFinalReport]. Uso interno del download: il percorso non va restituito al client.
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportFilePathQuery(IAuthenticationInfo authenticationInfo) : IRequest<string?>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <summary>Anno di riferimento.</summary>
    public int Year { get; set; }

    /// <summary>Trimestre di riferimento (1-4).</summary>
    public int Quarter { get; set; }
}
