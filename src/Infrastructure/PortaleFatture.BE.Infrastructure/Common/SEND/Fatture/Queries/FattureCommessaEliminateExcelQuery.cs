﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureCommessaEliminateExcelQuery(IAuthenticationInfo authenticationInfo) : IRequest<List<IEnumerable<FattureCommessaExcelDto>>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? IdEnti { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
}