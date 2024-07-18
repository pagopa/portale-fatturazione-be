using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;

public class DatiModuloCommessaParzialiTotaleResponse
{
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }

    [Column("FKIdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("FkIdStato")]
    public string? Stato { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public string? Totale { get; set; }
    public string? Digitale { get; set; }
    public string? AnalogicoARNazionali { get; set; }
    public string? AnalogicoARInternazionali { get; set; }
    public string? Analogico890Nazionali { get; set; }
    public bool Modifica { get; set; }
} 