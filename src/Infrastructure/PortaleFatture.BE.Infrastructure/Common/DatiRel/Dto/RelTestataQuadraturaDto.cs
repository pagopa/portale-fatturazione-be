using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.DatiRel;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

public sealed class RelTestataQuadraturaDto
{
    [JsonPropertyOrder(-1)]
    public List<RelQuadraturaDto>? Quadratura { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}