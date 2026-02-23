using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

 
public class FattureEmesseQuery(IAuthenticationInfo authenticationInfo) : IRequest<FattureDocContabiliDtoList>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? IdEnte => AuthenticationInfo.IdEnte;

    public string[]? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public DateTime[]? DateFattura { get; set; }
}