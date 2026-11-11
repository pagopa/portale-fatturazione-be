using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Righe del dettaglio di un documento emesso APP IO (contratto e trimestre), da
/// [be].[vwAppioFinalReportDetails]. Serve sia la pagina di dettaglio sia il suo download.
/// </summary>
/// <param name="authenticationInfo">Le informazioni di autenticazione dell'utente che effettua la richiesta.</param>
public class AppIoFinancialReportQueryGetDettaglio(IAuthenticationInfo authenticationInfo)
    : IRequest<IEnumerable<AppIoFinancialReportDettaglioRigaDto>>
{
    /// <summary>
    /// Le informazioni di autenticazione dell'utente che effettua la richiesta.
    /// </summary>
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    /// <summary>
    /// Contratto (onboardingtokenid) del documento.
    /// </summary>
    public string? ContractId { get; set; }

    /// <summary>
    /// Trimestre del documento, nel formato 'AAAA_T'.
    /// </summary>
    public string? YearQuarter { get; set; }
}
