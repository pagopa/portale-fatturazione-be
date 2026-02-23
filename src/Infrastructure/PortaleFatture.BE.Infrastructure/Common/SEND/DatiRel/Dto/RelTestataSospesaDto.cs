using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

public sealed class RelTestataSospesaDto
{
    [JsonPropertyOrder(-1)]
    public List<SimpleRelTestata>? RelTestate { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}