using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;

public sealed class OrchestratoreByDateQuery(IAuthenticationInfo authenticationInfo) : IRequest<OrchestratoreDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public DateTime? Init { get; set; }
    public DateTime? End { get; set; }
    public short[]? Stati { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; }
    public int? Ordinamento { get; set; } = 0; 
    public string[]? Tipologie { get; set; } 
    public string[]? Fasi { get; set; }
} 