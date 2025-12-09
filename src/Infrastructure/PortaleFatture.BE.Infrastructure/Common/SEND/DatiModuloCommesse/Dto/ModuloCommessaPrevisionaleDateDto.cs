using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

public class ModuloCommessaPrevisionaleDateDto
{
    [JsonPropertyOrder(-1)]
    public IList<ModuloCommessaPrevisionaleByAnnoDto>? ModuliCommessa { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}


public class ModuloCommessaPrevisionaleTotaliDateDto
{
    [JsonPropertyOrder(-1)]
    public IList<ModuloCommessaPrevisionaleTotaleDto>? ModuliCommessa { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}