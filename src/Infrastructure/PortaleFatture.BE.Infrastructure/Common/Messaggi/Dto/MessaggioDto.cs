using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;


namespace PortaleFatture.BE.Infrastructure.Common.Messaggi.Dto;

public class MessaggioDto
{
    public long IdMessaggio { get; set; }
    public string? IdEnte { get; set; }
    public string? IdUtente { get; set; }
    public string? Json { get; set; }
    public int Anno { get; set; }
    public int? Mese { get; set; }
    public string? Prodotto { get; set; }
    public string? GruppoRuolo { get; set; }
    public string? Auth { get; set; }
    public string? Stato { get; set; }
    public DateTime DataInserimento { get; set; }
    public DateTime? DataStepCorrente { get; set; }
    public string? LinkDocumento { get; set; }
    public string? TipologiaDocumento { get; set; }
    public string? CategoriaDocumento { get; set; }
    public bool Lettura { get; set; }
    public string? Hash { get; set; }
    public string? Rhash { get; set; }
    public string? ContentType { get; set; } 
    public string? ContentLanguage { get; set; } 
    public long? IdReport { get; set; }
}

public sealed class MessaggioListaDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<MessaggioDto>? Messaggi { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}