using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

public sealed class EnteQueryGetByRecapitistiConsolidatori(IAuthenticationInfo? authenticationInfo, string? tipo) : IRequest<IEnumerable<Ente>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Tipo { get; internal set; } = tipo;
}