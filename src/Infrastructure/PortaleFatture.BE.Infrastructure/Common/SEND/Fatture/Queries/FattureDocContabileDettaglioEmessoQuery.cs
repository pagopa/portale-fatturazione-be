using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureDocContabileDettaglioEmessoQuery : IRequest<IEnumerable<FatturaDocContabileEmessoDettaglioDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }

    public FattureDocContabileDettaglioEmessoQuery(IAuthenticationInfo authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }

    public string? IdEnte => AuthenticationInfo.IdEnte;

    public long? IdFattura { get; set; }
}