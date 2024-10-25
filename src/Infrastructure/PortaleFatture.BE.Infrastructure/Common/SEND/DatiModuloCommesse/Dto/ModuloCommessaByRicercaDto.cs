using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

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
    [Header(caption: "identificativo SC", Order = 1)]
    public string? IdEnte { get; set; }

    [Header(caption: "ragione sociale ente", Order = 2)]
    [Column("description")]
    public string? RagioneSociale { get; set; }

    [Header(caption: "codice fiscale", Order = 3)]
    [Column("vatnumber")]
    public string? CodiceFiscale { get; set; }

    [Header(caption: "prodotto", Order = 4)]
    [Column("FkProdotto")]
    public string? Prodotto { get; set; }

    [Header(caption: "Tipo Spedizione", Order = 5)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneDigitale { get; set; }


    [Header(caption: "N. Notifiche NZ", Order = 6)]

    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliDigitale { get; set; }


    [Header(caption: "N. Notifiche INT", Order = 7)]

    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliDigitale { get; set; }


    [Header(caption: "Tipo Spedizione", Order = 8)]

    [Column("TipoSpedizione")]
    public string? TipoSpedizioneAnalogicoAR { get; set; }

    [Header(caption: "N. Notifiche NZ", Order = 9)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogicoAR { get; set; }

    [Header(caption: "N. Notifiche INT", Order = 10)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogicoAR { get; set; }

    [Header(caption: "Tipo Spedizione", Order = 11)]
    [Column("TipoSpedizione")]
    public string? TipoSpedizioneAnalogico890 { get; set; }

    [Header(caption: "N. Notifiche NZ", Order = 12)]
    [Column("NumeroNotificheNazionali")]
    public int NumeroNotificheNazionaliAnalogico890 { get; set; }

    [Header(caption: "N. Notifiche INT", Order = 13)]
    [Column("NumeroNotificheInternazionali")]
    public int NumeroNotificheInternazionaliAnalogico890 { get; set; }

    [Header(caption: "Totale Spedizioni Analogiche", Order = 14)]
    [Column("TotaleCategoria")]
    public decimal TotaleCategoriaAnalogico { get; set; }

    [Header(caption: "Totale Spedizioni Digitali", Order = 15)]
    [Column("TotaleCategoria")]
    public decimal TotaleCategoriaDigitale { get; set; }

    [Header(caption: "Anno", Order = 16)]
    [Column("Anno")]
    public int Anno { get; set; }

    [Header(caption: "Mese", Order = 17)]
    [JsonIgnore]
    public string? SMese { get { return Mese.GetMonth(); } }

    [Column("Mese")]
    public int Mese { get; set; }

    [Header(caption: "Totale Analogiche Lordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleAnalogicoLordo { get; set; }

    [Header(caption: "Totale Digitali Lordo", Order = 18)]
    [Column("Totale")]
    public decimal TotaleDigitaleLordo { get; set; }

    [Header(caption: "Totale Lordo", Order = 19)]
    [Column("Totale")]
    public decimal TotaleLordo { get; set; }

    [Header(caption: "tipo contratto", Order = 20)]
    [JsonIgnore]
    public string? SIdTipoContratto { get { return IdTipoContratto.Map(); } }

    [Column("IdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("Stato")]
    [Header(caption: "Stato", Order = 21)]
    public string? Stato { get; set; }
}


