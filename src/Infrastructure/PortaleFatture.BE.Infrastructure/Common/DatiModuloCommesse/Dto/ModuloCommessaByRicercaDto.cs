using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Dto;

public sealed class ModuloCommessaByRicercaDto
{
    [Column("Description")]
    [JsonPropertyOrder(-6)]
    public string? Key
    {
        get
        {
            return $"{IdEnte}_{Prodotto}_{Anno}_{Mese}";
        }
    }

    [HeaderAttribute(caption: "Ragione Sociale", Order = 1)]
    [Column("description")]
    public string? RagioneSociale { get; set; }

    [HeaderAttribute(caption: "Codice Fiscale", Order = 2)]
    [Column("vatnumber")]
    public string? CodiceFiscale { get; set; }

    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }


    [HeaderAttribute(caption: "Prodotto", Order = 3)]
    [Column("FkProdotto")]
    public string? Prodotto { get; set; }

    [HeaderAttribute(caption: "Tipo Sped. Digit.", Order = 4)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneDigitale { get; set; }

    [HeaderAttribute(caption: "N. Notifiche NZ Digit.", Order = 5)]

    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliDigitale { get; set; }


    [HeaderAttribute(caption: "N. Notifiche INT Digit.", Order = 6)]

    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliDigitale { get; set; }


    [HeaderAttribute(caption: "Tipo Sped. AnalogicoAR", Order = 7)]

    [Column("TipoSpedizione")] 
    public string? TipoSpedizioneAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "N. Notifiche NZ AnalogicoAR", Order = 8)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "N. Notifiche INT AnalogicoAR", Order = 9)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "Tipo Sped. Analogico890", Order = 10)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneAnalogico890 { get; set; }

    [HeaderAttribute(caption: "N. Notifiche NZ Analogico890", Order = 11)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogico890 { get; set; } 

    [HeaderAttribute(caption: "N. Notifiche INT Analogico890", Order = 12)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogico890 { get; set; }

    [HeaderAttribute(caption: "Totale Categoria Analogico", Order = 13)]
    [Column("TotaleCategoria")]
    public decimal TotaleCategoriaAnalogico { get; set; }

    [HeaderAttribute(caption: "Totale Categoria Digitale", Order = 14)]
    [Column("TotaleCategoria")]
    public int TotaleCategoriaDigitale { get; set; }

    [HeaderAttribute(caption: "Anno", Order = 15)]
    [Column("Anno")]
    public int Anno { get; set; }

    [HeaderAttribute(caption: "Mese", Order = 16)]
    [JsonIgnore]
    public string? SMese { get; set; }

    [Column("Mese")]
    public int Mese { get; set; }

    [HeaderAttribute(caption: "Totale Analogico Lordo", Order = 17)]
    [Column("Totale")]
    public decimal TotaleAnalogicoLordo { get; set; }

    [HeaderAttribute(caption: "Totale Digitale Lordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleDigitaleLordo { get; set; }

    [HeaderAttribute(caption: "Totale Lordo", Order = 19)]
    [Column("Totale")]
    public decimal TotaleLordo { get; set; }

    [HeaderAttribute(caption: "Tipo Contratto", Order = 20)]
    [JsonIgnore]
    public string? SIdTipoContratto { get; set; }

    [Column("IdTipoContratto")]
    public long IdTipoContratto { get; set; }
}
