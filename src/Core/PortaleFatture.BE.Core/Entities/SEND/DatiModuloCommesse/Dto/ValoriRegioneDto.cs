using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
public class ValoriRegioneDto
{
    [JsonPropertyName("provincia")]
    public string? Provincia { get; set; }

    [JsonPropertyName("regione")]
    public string Regione { get; set; } = null!;

    [JsonPropertyName("istatProvincia")]
    public string? IstatProvincia { get; set; }

    [JsonPropertyName("istatRegione")]
    public string? IstatRegione { get; set; }

    [JsonPropertyName("ar")]
    public int? AR { get; set; }

    [JsonPropertyName("890")]
    public int? A890 { get; set; }

    [JsonPropertyName("isRegione")]
    public int IsRegione { get; set; }

    [JsonPropertyName("obbligatorio")]
    public int Obbligatorio { get; set; } 

    [JsonIgnore]
    public string? Internalistitutionid { get; set; }

    [JsonIgnore]
    public int Anno { get; set; }

    [JsonIgnore]
    public int Mese { get; set; }

    [JsonPropertyName("calcolato")]
    public int calcolato { get; set; }
}