using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;

public class DatiModuloCommessaCreateRequest
{
    public List<DatiModuloCommessaCreateSimpleRequest>? ModuliCommessa { get; set; }

}
public class DatiModuloCommessaCreateSimpleRequest
{
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }
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