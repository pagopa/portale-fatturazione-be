using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;

public sealed class PSPListDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<PSP>? PSPs { get; set; } 

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}