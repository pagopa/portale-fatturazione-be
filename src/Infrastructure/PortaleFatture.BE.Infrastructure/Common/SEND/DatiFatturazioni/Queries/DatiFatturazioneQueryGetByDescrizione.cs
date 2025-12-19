using System.Text.Json.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByDescrizione(IAuthenticationInfo? authenticationInfo, string[]? idEnti, string? prodotto, string? profilo, int? idTipoContratto, int? page, int? size) : IRequest<DatiFatturazioneEnteWithCountDto>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? IdEnti { get; internal set; } = idEnti;
    public string? Prodotto { get; internal set; } = prodotto;
    public string? Profilo { get; internal set; } = profilo;
    public int? IdTipoContratto { get; set; } = idTipoContratto;
    public int? PageNumber { get; set; } = page;
    public int? PageSize { get; set; } = size;
}