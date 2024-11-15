﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

public class AzioneContestazioneQueryGetByIdNotifica(IAuthenticationInfo authenticationInfo, string? idNotifica) : IRequest<AzioneNotificaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? IdNotifica { get; internal set; } = idNotifica;
}