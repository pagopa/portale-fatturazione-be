using System.Text.Json.Serialization;

namespace PortaleFatture.BE.ImportModuloCommessaAderenti;

public class MooduloCommessaAderentiDto
{
    [JsonPropertyName("dataExport")]
    public string? DataExport { get; set; }

    [JsonPropertyName("internalistitutionid")]
    public string? Internalistitutionid { get; set; }

    [JsonPropertyName("segmento")]
    public string? Segmento { get; set; }

    [JsonPropertyName("macrocategoriaVendita")]
    public string? MacrocategoriaVendita { get; set; }

    [JsonPropertyName("sottocategoriaVendita")]
    public string? SottocategoriaVendita { get; set; }

    [JsonPropertyName("provincia")]
    public string? Provincia { get; set; }

    [JsonPropertyName("regione")]
    public string? Regione { get; set; }
}