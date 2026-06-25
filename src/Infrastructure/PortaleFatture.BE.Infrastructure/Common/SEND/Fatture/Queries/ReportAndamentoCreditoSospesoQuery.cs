using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

/// <summary>
/// Query per ottenere il report di andamento del credito sospeso, con filtri dinamici per anno, mese e tipologia di fattura.
/// </summary>
/// <param name="authenticationInfo">Informazioni di autenticazione dell'utente.</param>
public class ReportAndamentoCreditoSospesoQuery(IAuthenticationInfo authenticationInfo)
    : IRequest<IEnumerable<ReportAndamentoCreditoSospesoDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string[]? TipologiaFattura { get; set; }
}