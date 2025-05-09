using System.Text.Json.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

public class CreateApyLogCommand(IAuthenticationInfo? authenticationInfo) : IRequest<int?>
{ 
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;  
    public string? FkIdEnte { get; set; } = authenticationInfo!.IdEnte;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow.ItalianTime(); 
    public string? FunctionName { get; set; } 
    public string? Stage { get; set; } 
    public string? Method { get; set; } 
    public string? Payload { get; set; } 
    public string? Uri { get; set; } 
    public string? IpAddress { get; set; } 
    public string? Id { get; set; }
}