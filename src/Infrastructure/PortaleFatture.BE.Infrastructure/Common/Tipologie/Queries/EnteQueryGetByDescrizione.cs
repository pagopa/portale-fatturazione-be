using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public sealed class EnteQueryGetByDescrizione(AuthenticationInfo? authenticationInfo, string? descrizione) : IRequest<IEnumerable<Ente>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Descrizione { get; internal set; } = descrizione;  
}