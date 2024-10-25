using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

public sealed class RelTestataQuadraturaDto
{
    [JsonPropertyOrder(-1)]
    public List<RelQuadraturaDto>? Quadratura { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}