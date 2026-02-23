using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureDocContabileDettaglioQuery : IRequest<IEnumerable<FattureDocContabiliDettaglioDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }

    public FattureDocContabileDettaglioQuery(IAuthenticationInfo authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }

    public string? IdEnte => AuthenticationInfo.IdEnte;

    public long? IdFattura { get; set; }
}