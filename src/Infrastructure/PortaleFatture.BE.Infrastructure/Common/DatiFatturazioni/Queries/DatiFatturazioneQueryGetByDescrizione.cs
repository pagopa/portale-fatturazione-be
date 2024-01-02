using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByDescrizione(IAuthenticationInfo? authenticationInfo, string descrizione, string? prodotto, string? profilo) : IRequest<IEnumerable<DatiFatturazioneEnteDto>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Descrizione { get; internal set; } = descrizione;
    public string? Prodotto { get; internal set; } = prodotto;
    public string? Profilo { get; internal set; } = profilo;
} 