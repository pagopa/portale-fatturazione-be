﻿using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public sealed class ProfiloQueryGetAll : IRequest<IEnumerable<string>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}