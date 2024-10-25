using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

public sealed class EnteQueryGetByRagioneSociale(AuthenticationInfo? authenticationInfo, string? descrizione, string? prodotto, string? profilo) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Descrizione { get; internal set; } = descrizione;
    public string? Prodotto { get; internal set; } = prodotto;
    public string? Profilo { get; internal set; } = profilo;
}