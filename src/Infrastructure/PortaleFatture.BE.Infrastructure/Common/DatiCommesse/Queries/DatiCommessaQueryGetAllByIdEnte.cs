﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries;

public class DatiCommessaQueryGetAllByIdEnte : IRequest<IEnumerable<DatiCommessa>?>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public string? IdEnte { get; set; }
} 