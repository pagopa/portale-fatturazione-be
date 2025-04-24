using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

public sealed class CreateORModifyApiKeyCommand(IAuthenticationInfo? authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? PreviousApiKey { get; set; }
    public string? ApiKey { get; set; }  
    public bool? Attiva { get; set; } = false;
    public string? FkIdEnte { get; set; } = authenticationInfo!.IdEnte;
    public DateTime? DataCreazione { get; set; } = DateTime.UtcNow.ItalianTime();
    public DateTime? DataModifica { get; set; } = DateTime.UtcNow.ItalianTime();  
    public bool? Refresh { get; set; }
} 