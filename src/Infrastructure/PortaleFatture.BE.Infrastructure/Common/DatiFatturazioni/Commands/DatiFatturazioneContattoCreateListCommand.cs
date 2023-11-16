﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;

public sealed class DatiFatturazioneContattoCreateListCommand : IRequest<DatiFatturazioneContatto>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }

    List<DatiFatturazioneContattoCreateCommand>? DatiFatturazioneContattoLista { get; set; }
}