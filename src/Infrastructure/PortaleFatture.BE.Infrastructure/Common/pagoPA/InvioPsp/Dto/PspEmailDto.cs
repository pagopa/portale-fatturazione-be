namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Dto;

using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

public class PspEmailDto
{
    [HeaderPagoPA(caption: "Psp", Order = 1)]
    [JsonPropertyName("psp")]
    public string? Psp { get; set; }

    [HeaderPagoPA(caption: "Tipologia", Order = 2)]
    [JsonPropertyName("tipologia")]
    public string? Tipologia { get; set; }

    [HeaderPagoPA(caption: "Anno", Order = 3)]
    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [HeaderPagoPA(caption: "Trimestre", Order = 4)]
    [JsonPropertyName("trimestre")]
    public string? Trimestre { get; set; }

    [HeaderPagoPA(caption: "Data Evento", Order = 5, Style = XCellStyle.StandardDate)]
    [JsonPropertyName("dataEvento")]
    public DateTime DataEvento { get; set; }

    [HeaderPagoPA(caption: "Email", Order = 6)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [HeaderPagoPA(caption: "Messaggio", Order = 7)]
    [JsonPropertyName("messaggio")]
    public string? Messaggio { get; set; }

    [HeaderPagoPA(caption: "Ragione Sociale", Order = 8)]
    [JsonPropertyName("ragioneSociale")]
    public string? RagioneSociale { get; set; }

    [HeaderPagoPA(caption: "Invio", Order = 9)]
    [JsonPropertyName("invio")]
    public bool Invio { get; set; }
}