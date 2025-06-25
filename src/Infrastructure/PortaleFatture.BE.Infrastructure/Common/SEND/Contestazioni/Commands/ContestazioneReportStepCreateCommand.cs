using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands;

public class ContestazioneReportStepCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; private set; } = authenticationInfo; 
    public DateTime DataCompletamento { get; set; } = DateTime.UtcNow.ItalianTime();
    public short Step { get; set; } = TipologiaStatoMessaggio.CaricamentoFile; 
    public string? NomeDocumento { get; set; }
    public string? LinkDocumento { get; set; }
    public string? Storage { get; set; }  
    public long IdReport { get; set; }  
}