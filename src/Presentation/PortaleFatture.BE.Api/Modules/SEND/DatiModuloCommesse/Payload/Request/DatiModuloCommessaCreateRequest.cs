using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;

 
public class DatiModuloCommessaCreateRequest
{
    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [JsonPropertyName("mese")]
    public int Mese { get; set; }

    [JsonPropertyName("moduliCommessa")]
    public List<DatiModuloCommessaCreateSimpleRequest>? ModuliCommessa { get; set; }

    [JsonPropertyName("valoriRegione")]
    public List<ValoriRegioneDto>? ValoriRegioni { get; set; } 
}

public class DatiModuloCommessaCreateSimpleRequest
{
    [JsonPropertyName("numeroNotificheNazionali")]
    public int NumeroNotificheNazionali { get; set; }

    [JsonPropertyName("numeroNotificheInternazionali")]
    public int NumeroNotificheInternazionali { get; set; }

    [JsonPropertyName("idTipoSpedizione")]
    public int IdTipoSpedizione { get; set; }
}

public class DatiModuloCommessaPagoPACreateRequest
{
    [Required]
    public string? IdEnte { get; set; }

    [Required]
    public string? Prodotto { get; set; }

    [Required]
    public long IdTipoContratto { get; set; }

    [Required]
    public List<DatiModuloCommessaCreateSimpleRequest>? ModuliCommessa { get; set; }
    public bool? Fatturabile { get; set; }
}