using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;

 
public class DatiModuloCommessaCreateRequest
{
    [JsonPropertyName("moduliCommessa")]
    public List<DatiModuloCommessaCreateSimpleRequest>? ModuliCommessa { get; set; }

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