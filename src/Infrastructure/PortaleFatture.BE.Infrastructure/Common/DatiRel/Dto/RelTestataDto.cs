using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.DatiRel;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

public sealed class RelTestataDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SimpleRelTestata>? RelTestate { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}