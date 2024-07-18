using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public sealed class EnteQueryGetByRecapitistiConsolidatori(IAuthenticationInfo? authenticationInfo, string? tipo) : IRequest<IEnumerable<Ente>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Tipo { get; internal set; } = tipo;  
}