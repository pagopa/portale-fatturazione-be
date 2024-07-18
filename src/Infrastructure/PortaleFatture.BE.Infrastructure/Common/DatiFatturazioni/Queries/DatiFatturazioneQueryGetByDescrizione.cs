﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByDescrizione(IAuthenticationInfo? authenticationInfo, string[]? idEnti, string? prodotto, string? profilo, int? top) : IRequest<IEnumerable<DatiFatturazioneEnteDto>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? IdEnti { get; internal set; } = idEnti;
    public string? Prodotto { get; internal set; } = prodotto;
    public string? Profilo { get; internal set; } = profilo; 
    public int? Top { get; internal set; } = top;
} 