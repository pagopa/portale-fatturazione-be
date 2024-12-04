﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

public class SpedizioneQueryGetAll : IRequest<IEnumerable<CategoriaSpedizione>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}