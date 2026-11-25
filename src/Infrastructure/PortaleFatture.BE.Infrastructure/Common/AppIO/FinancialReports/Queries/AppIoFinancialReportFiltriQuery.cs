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
/// Percorsi sul blob storage dei file dei financial report APP IO di uno o più trimestri, da
/// [be].[vwAppioFinalReport]. Uso interno del download: i percorsi non vanno restituiti al client.
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportFilePathsQuery(IAuthenticationInfo authenticationInfo) : IRequest<IReadOnlyDictionary<string, string>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <summary>Trimestri nel formato 'AAAA_T'.</summary>
    public string[] YearQuarters { get; set; } = [];
}
