﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByDescrizione(IAuthenticationInfo? authenticationInfo, string descrizione, string? prodotto, string? profilo, int? top) : IRequest<IEnumerable<DatiFatturazioneEnteDto>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Descrizione { get; internal set; } = descrizione;
    public string? Prodotto { get; internal set; } = prodotto;
    public string? Profilo { get; internal set; } = profilo; 
    public int? Top { get; internal set; } = top;
} 