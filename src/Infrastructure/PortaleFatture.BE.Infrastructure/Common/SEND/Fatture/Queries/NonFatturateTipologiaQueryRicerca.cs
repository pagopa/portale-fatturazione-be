using System.Collections.Generic;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class NonFatturateTipologiaQueryRicerca(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<AnniMesiTipologiaDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? TipologiaFattura { get; set; }
    public int Inviata { get; set; } = 0; 
}