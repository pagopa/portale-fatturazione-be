using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

public class FattureWhiteListFattureAggiungiCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int Anno { get; set; }
    public int[]? Mesi { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }
    public DateTime? DataInizio { get; set; } = DateTime.UtcNow.ItalianTime(); 
    public string? IdUtente { get; set; } = authenticationInfo!.Id;
} 