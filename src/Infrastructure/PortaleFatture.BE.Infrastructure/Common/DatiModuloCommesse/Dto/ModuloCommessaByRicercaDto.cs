using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;
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

    [Column("FkIdEnte")]
    [HeaderAttribute(caption: "IdEnte", Order = 1)]
    public string? IdEnte { get; set; }

    [HeaderAttribute(caption: "RagioneSociale", Order = 2)]
    [Column("description")]
    public string? RagioneSociale { get; set; }

    [HeaderAttribute(caption: "CodiceFiscale", Order = 3)]
    [Column("vatnumber")]
    public string? CodiceFiscale { get; set; } 

    [HeaderAttribute(caption: "Prodotto", Order = 4)]
    [Column("FkProdotto")]
    public string? Prodotto { get; set; }

    [HeaderAttribute(caption: "TipoSpedizioneDigitale", Order = 5)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneDigitale { get; set; }


    [HeaderAttribute(caption: "NumeroNotificheNazionaliDigitale", Order = 6)]

    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliDigitale { get; set; }


    [HeaderAttribute(caption: "NumeroNotificheInternazionaliDigitale", Order = 7)]

    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliDigitale { get; set; }


    [HeaderAttribute(caption: "TipoSpedizioneAnalogicoAR", Order = 8)]

    [Column("TipoSpedizione")] 
    public string? TipoSpedizioneAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "NumeroNotificheNazionaliAnalogicoAR", Order = 9)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "NumeroNotificheInternazionaliAnalogicoAR", Order = 10)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "TipoSpedizioneAnalogico890", Order = 11)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneAnalogico890 { get; set; }

    [HeaderAttribute(caption: "NumeroNotificheNazionaliAnalogico890", Order = 12)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogico890 { get; set; } 

    [HeaderAttribute(caption: "NumeroNotificheInternazionaliAnalogico890", Order = 13)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogico890 { get; set; }

    [HeaderAttribute(caption: "TotaleCategoriaAnalogico", Order = 14)]
    [Column("TotaleCategoria")]
    public decimal TotaleCategoriaAnalogico { get; set; }

    [HeaderAttribute(caption: "TotaleCategoriaDigitale", Order = 15)]
    [Column("TotaleCategoria")]
    public int TotaleCategoriaDigitale { get; set; }

    [HeaderAttribute(caption: "Anno", Order = 16)]
    [Column("Anno")]
    public int Anno { get; set; }

    [HeaderAttribute(caption: "Mese", Order = 17)]
    [JsonIgnore]
    public string? SMese { get { return Mese.GetMonth(); } }

    [Column("Mese")]
    public int Mese { get; set; }

    [HeaderAttribute(caption: "TotaleAnalogicoLordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleAnalogicoLordo { get; set; }

    [HeaderAttribute(caption: "TotaleDigitaleLordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleDigitaleLordo { get; set; }

    [HeaderAttribute(caption: "TotaleLordo", Order = 19)]
    [Column("Totale")]
    public decimal TotaleLordo { get; set; }

    [HeaderAttribute(caption: "IdTipoContratto", Order = 20)]
    [JsonIgnore]
    public string? SIdTipoContratto { get { return IdTipoContratto.Map(); } }

    [Column("IdTipoContratto")]
    public long IdTipoContratto { get; set; }
}


