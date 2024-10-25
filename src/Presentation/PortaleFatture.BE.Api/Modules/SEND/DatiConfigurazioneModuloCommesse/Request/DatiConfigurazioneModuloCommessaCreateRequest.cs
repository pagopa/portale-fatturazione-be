using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiConfigurazioneModuloCommesse.Request;

public record DatiConfigurazioneModuloCommessaCreateRequest
{
    [Required]
    public int IdTipoContratto { get; set; }

    [Required]
    public string? Prodotto { get; set; }

    public List<DatiConfigurazioneModuloCommessaCreateCategoriaRequest>? Categorie { get; set; }
    public List<DatiConfigurazioneModuloCommessaCreateTipoSpedizioneRequest>? Tipi { get; set; }
}

public record DatiConfigurazioneModuloCommessaCreateCategoriaRequest
{
    [Required]
    public int IdCategoriaSpedizione { get; set; }

    [Required]
    public int Percentuale { get; set; }
}

public record DatiConfigurazioneModuloCommessaCreateTipoSpedizioneRequest
{
    [Required]
    public int IdTipoSpedizione { get; set; }

    [Required]
    public decimal PrezzoMedioNotificaNazionale { get; set; }

    [Required]
    public decimal PrezzoMedioNotificaInternazionale { get; set; }
}