﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
public class DatiModuloCommessaDocumentoQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<ModuloCommessaDocumentoDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int? AnnoValidita { get; set; } 
    public int? MeseValidita { get; set; }
} 