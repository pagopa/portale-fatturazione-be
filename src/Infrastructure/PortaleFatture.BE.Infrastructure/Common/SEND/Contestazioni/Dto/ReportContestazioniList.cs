using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

public sealed class ReportContestazioniList
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<ReportContestazioni>? Reports { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}