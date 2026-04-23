using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureDettaglioEmessoPdfQuery : FattureDocContabileDettaglioEmessoQuery
{
    public FattureDettaglioEmessoPdfQuery(IAuthenticationInfo authenticationInfo)
        : base(authenticationInfo)
    {
    }
}