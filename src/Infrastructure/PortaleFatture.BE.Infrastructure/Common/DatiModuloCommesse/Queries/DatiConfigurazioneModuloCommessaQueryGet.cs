﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;

public class DatiConfigurazioneModuloCommessaQueryGet : IRequest<DatiConfigurazioneModuloCommessa>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public string? Prodotto { get; set; }
    public long IdTipoContratto { get; set; }
} 