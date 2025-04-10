﻿using MediatR;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;

public class ScadenziarioService(IMediator handler, ILogger<ScadenziarioService> logger) : IScadenziarioService
{
    private readonly IMediator _handler = handler;
    private readonly ILogger<ScadenziarioService> _logger = logger;

    public async Task<(bool, Scadenziario)> GetScadenziario(IAuthenticationInfo authenticationInfo, TipoScadenziario tipo, int anno, int mese)
    {
        return await _handler.Send(new ScadenziarioQueryGetByTipo(authenticationInfo, tipo, anno, mese));
    }
}