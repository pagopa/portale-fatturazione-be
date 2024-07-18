using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Storici.Commands;

public class StoricoCreateCommand(IAuthenticationInfo authenticationInfo,
    DateTime DataEvento,
    string? DescrizioneEvento,
    string? JsonTransazione) : IRequest<int>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public DateTime DataEvento { get; internal set; } = DataEvento;
    public string? DescrizioneEvento { get; internal set; } = DescrizioneEvento;
    public string? JsonTransazione { get; internal set; } = JsonTransazione;
} 