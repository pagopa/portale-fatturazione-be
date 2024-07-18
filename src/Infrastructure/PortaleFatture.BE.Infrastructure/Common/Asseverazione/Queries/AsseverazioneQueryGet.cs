﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;

public class AsseverazioneQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<EnteAsserverazioneDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}