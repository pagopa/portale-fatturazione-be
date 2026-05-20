using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;

public class RelTestataDettaglioDto
{
    public string? IdTestata
    {
        get
        {
            return $"{IdEnte}_{IdContratto}_{TipologiaFattura!.Replace(" ", "-")}_{Anno}_{Mese}";
        }
    }

    [Column("IdEnte")]
    public string? IdEnte { get; set; }

    [Column("RagioneSociale")]
    public string? RagioneSociale { get; set; }

    [Column("DataDocumento")]
    public DateTime? DataDocumento { get; set; }

    [Column("IdDocumento")]
    public string? IdDocumento { get; set; }

    [Column("Cup")]
    public string? Cup { get; set; }

    [Column("IdContratto")]
    public string? IdContratto { get; set; }

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [Column("anno")]
    public string? Anno { get; set; }

    [Column("mese")]
    public string? Mese { get; set; }

    [Column("TotaleAnalogico")]
    public decimal TotaleAnalogico { get; set; }

    [Column("TotaleDigitale")]
    public decimal TotaleDigitale { get; set; }

    [Column("TotaleNotificheAnalogiche")]
    public int TotaleNotificheAnalogiche { get; set; }

    [Column("TotaleNotificheDigitali")]
    public int TotaleNotificheDigitali { get; set; }

    [Column("Anticipo_StornoAnalogico")]
    public decimal? Anticipo_StornoAnalogico { get; set; }

    [Column("Anticipo_StornoDigitale")]
    public decimal? Anticipo_StornoDigitale { get; set; }

    [Column("Acconto_StornoAnalogico")]
    public decimal? Acconto_StornoAnalogico { get; set; }

    [Column("Acconto_StornoDigitale")]
    public decimal? Acconto_StornoDigitale { get; set; }

    [Column("Anticipo_StornoTotale")]
    public decimal? Anticipo_StornoTotale { get; set; }

    [Column("Acconto_StornoTotale")]
    public decimal? Acconto_StornoTotale { get; set; }

    [Column("StornoTotale")]
    public decimal? StornoTotale { get; set; }

    [Column("Totale")]
    public decimal Totale { get; set; }

    public bool DatiFatturazione { get; set; }

    [Column("Iva")]
    public decimal Iva { get; set; }

    [Column("TotaleAnalogicoIva")]
    public decimal TotaleAnalogicoIva { get; set; }

    [Column("TotaleDigitaleIva")]
    public decimal TotaleDigitaleIva { get; set; }

    [Column("TotaleIva")]
    public decimal TotaleIva { get; set; }

    [Column("AsseverazioneTotaleAnalogico")]
    public decimal AsseverazioneTotaleAnalogico { get; set; }

    [Column("AsseverazioneTotaleDigitale")]
    public decimal AsseverazioneTotaleDigitale { get; set; }

    [Column("AsseverazioneTotaleNotificheAnalogiche")]
    public int AsseverazioneTotaleNotificheAnalogiche { get; set; }

    [Column("AsseverazioneTotaleNotificheDigitali")]
    public int AsseverazioneTotaleNotificheDigitali { get; set; }

    [Column("AsseverazioneTotale")]
    public decimal AsseverazioneTotale { get; set; }

    [Column("AsseverazioneTotaleAnalogicoIva")]
    public decimal AsseverazioneTotaleAnalogicoIva { get; set; }

    [Column("AsseverazioneTotaleDigitaleIva")]
    public decimal AsseverazioneTotaleDigitaleIva { get; set; }

    [Column("AsseverazioneTotaleIva")]
    public decimal AsseverazioneTotaleIva { get; set; }

    public string Firmata
    {
        get { return Caricata.MapRelTestata(); }
    }

    [Column("Caricata")]
    public byte Caricata { get; set; }
}