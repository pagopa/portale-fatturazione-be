﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureInvioSapQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<FatturaInvioSap>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Anno { get; set; }
    public int? Mese { get; set; }
}