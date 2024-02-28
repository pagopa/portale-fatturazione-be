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
    [HeaderAttribute(caption: "identificativo SC", Order = 1)]
    public string? IdEnte { get; set; }

    [HeaderAttribute(caption: "ragione sociale ente", Order = 2)]
    [Column("description")]
    public string? RagioneSociale { get; set; }

    [HeaderAttribute(caption: "codice fiscale", Order = 3)]
    [Column("vatnumber")]
    public string? CodiceFiscale { get; set; } 

    [HeaderAttribute(caption: "prodotto", Order = 4)]
    [Column("FkProdotto")]
    public string? Prodotto { get; set; }

    [HeaderAttribute(caption: "Tipo Spedizione", Order = 5)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneDigitale { get; set; }


    [HeaderAttribute(caption: "N. Notifiche NZ", Order = 6)]

    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliDigitale { get; set; }


    [HeaderAttribute(caption: "N. Notifiche INT", Order = 7)]

    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliDigitale { get; set; }


    [HeaderAttribute(caption: "Tipo Spedizione", Order = 8)]

    [Column("TipoSpedizione")] 
    public string? TipoSpedizioneAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "N. Notifiche NZ", Order = 9)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "N. Notifiche INT", Order = 10)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogicoAR { get; set; }

    [HeaderAttribute(caption: "Tipo Spedizione", Order = 11)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneAnalogico890 { get; set; }

    [HeaderAttribute(caption: "N. Notifiche NZ", Order = 12)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogico890 { get; set; } 

    [HeaderAttribute(caption: "N. Notifiche INT", Order = 13)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogico890 { get; set; }

    [HeaderAttribute(caption: "Totale Spedizioni Analogiche", Order = 14)]
    [Column("TotaleCategoria")]
    public decimal TotaleCategoriaAnalogico { get; set; }

    [HeaderAttribute(caption: "Totale Spedizioni Digitali", Order = 15)]
    [Column("TotaleCategoria")]
    public decimal TotaleCategoriaDigitale { get; set; }

    [HeaderAttribute(caption: "Anno", Order = 16)]
    [Column("Anno")]
    public int Anno { get; set; }

    [HeaderAttribute(caption: "Mese", Order = 17)]
    [JsonIgnore]
    public string? SMese { get { return Mese.GetMonth(); } }

    [Column("Mese")]
    public int Mese { get; set; }

    [HeaderAttribute(caption: "Totale Analogiche Lordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleAnalogicoLordo { get; set; }

    [HeaderAttribute(caption: "Totale Digitali Lordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleDigitaleLordo { get; set; }

    [HeaderAttribute(caption: "Totale Lordo", Order = 19)]
    [Column("Totale")]
    public decimal TotaleLordo { get; set; }

    [HeaderAttribute(caption: "tipo contratto", Order = 20)]
    [JsonIgnore]
    public string? SIdTipoContratto { get { return IdTipoContratto.Map(); } }

    [Column("IdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("Stato")]
    [HeaderAttribute(caption: "Stato", Order = 21)]
    public string? Stato { get; set; }
}


