using DocumentFormat.OpenXml.InkML;
using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;

public class EnteAsserverazioneListImportCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public List<EnteAsserverazioneImportCreateCommand>? ListCommands { get; set; }
}

public class EnteAsserverazioneImportCreateCommand(DateTime timestamp)
{
    public DateTime? TimeStamp { get; internal set; } = timestamp;
    public string? IdEnte { get; set; }
    public string? RagioneSociale { get; set; }
    public DateTime? DataAsseverazione { get; set; }
    public bool? TipoAsseverazione { get; set; }
    public string? IdUtente { get; set; } 
    public string? Descrizione { get; set; }
}

