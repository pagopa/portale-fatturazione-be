using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.SelfCare;
namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

public class ModuloCommessaAggregateDto
{
    public Ente? Ente { get; set; }
    public DatiFatturazione? DatiFatturazione { get; set; }
    public IEnumerable<DatiModuloCommessa>? DatiModuloCommessa { get; set; }
    public DatiConfigurazioneModuloCommessa? DatiConfigurazioneModuloCommessa { get; set; }
    public ModuloCommessaByAnnoDto? DatiModuloCommessaTotale { get; set; }
}

public class ModuloCommessaDocumentoDto
{
    // fatt
    public string? Cup { get; set; } 
    public string? CodCommessa { get; set; }
    public DateTimeOffset DataDocumento { get; set; }
    public string? SplitPayment { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? TipoCommessa { get; set; }
    public string? Prodotto { get; set; }
    public string? Pec { get; set; }
    public DateTimeOffset? DataModifica { get; set; }
    public string? MeseAttivita { get; set; }
    public IEnumerable<DatiFatturazioneContatto>? Contatti { get; set; }
    //fat

    //ente 
    public string? Descrizione { get; set; }
    public string? PartitaIva { get; set; }
    public string? IndirizzoCompleto { get; set; }
    //ente 

    // modulo commessa
    public IEnumerable<DatiModuloCommessaTotaleDto>? DatiModuloCommessa { get; set; }
    // modulo commessa

    // modulo commessa totali
    public IEnumerable<DatiModuloCommessaTotaleCostoDto>? DatiModuloCommessaCosti { get; set; }
    // modulo commessa totali
}

public class DatiModuloCommessaTotaleDto
{
    public int TotaleNotifiche { get; set; }
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }
    public string? Tipo { get; set; }
    public int IdTipoSpedizione { get; set; }
}

public class DatiModuloCommessaTotaleCostoDto
{
    [JsonIgnore]
    public decimal Totale { get; set; } 

    [JsonPropertyName("Totale")]
    public string? TotaleValuta
    {
        get {return $"{Totale} €"; } 
    }

    public string? Descrizione { get; set; } 
}