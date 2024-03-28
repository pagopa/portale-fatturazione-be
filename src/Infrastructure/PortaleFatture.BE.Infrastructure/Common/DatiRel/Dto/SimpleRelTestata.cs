using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Core.Entities.DatiRel;

public class SimpleRelTestata
{
    public string? IdTestata
    {
        get
        {
            return new RelTestataKey(IdEnte, IdContratto, TipologiaFattura, Anno, Mese).ToString();
        }
    }
    public string? NomeTestata
    {
        get
        {
            return $"{RagioneSociale}_{IdTestata}";
        }
    }

    [Column("internal_organization_id")]
    [HeaderAttribute(caption: "IdEnte", Order = 1)]
    public string? IdEnte { get; set; }


    [HeaderAttribute(caption: "Ragione Sociale", Order = 2)]

    [Column("description")]
    public string? RagioneSociale { get; set; }


    [HeaderAttribute(caption: "IdContratto", Order = 3)]

    [Column("contract_id")]
    public string? IdContratto { get; set; }


    [HeaderAttribute(caption: "Tipologia Fattura", Order = 4)]

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [HeaderAttribute(caption: "Anno", Order = 5)]

    [Column("year")]
    public int? Anno { get; set; }


    [HeaderAttribute(caption: "Mese", Order = 6)]

    [Column("month")]
    public int? Mese { get; set; }


    [HeaderAttribute(caption: "Totale Imponibile Analogico €", Order = 11)]

    [Column("TotaleAnalogico")]
    public decimal TotaleAnalogico { get; set; }


    [HeaderAttribute(caption: "Totale Imponibile Digitale €", Order = 12)]

    [Column("TotaleDigitale")]
    public decimal TotaleDigitale { get; set; }


    [HeaderAttribute(caption: "N. Notifiche Analogiche", Order = 8)]
    [Column("TotaleNotificheAnalogiche")]
    public int TotaleNotificheAnalogiche { get; set; }


    [HeaderAttribute(caption: "N. Notifiche Digitali", Order = 9)]

    [Column("TotaleNotificheDigitali")]
    public int TotaleNotificheDigitali { get; set; }


    [HeaderAttribute(caption: "N. Totale Notifiche", Order = 10)]
    public int TotaleNotifiche
    {
        get
        {

            return TotaleNotificheAnalogiche + TotaleNotificheDigitali;


        }
    }

    [HeaderAttribute(caption: "Totale Imponibile €", Order = 13)]
    [Column("Totale")]
    public decimal Totale { get; set; }

    [HeaderAttribute(caption: "%Iva", Order = 14)]

    [Column("Iva")]
    public decimal Iva { get; set; }


    [HeaderAttribute(caption: "Totale Ivato Analogico €", Order = 15)]

    [Column("TotaleAnalogicoIva")]
    public decimal TotaleAnalogicoIva { get; set; }


    [HeaderAttribute(caption: "Totale Ivato Digitale €", Order = 16)]

    [Column("TotaleDigitaleIva")]
    public decimal TotaleDigitaleIva { get; set; }


    [HeaderAttribute(caption: "Totale Ivato €", Order = 17)]

    [Column("TotaleIva")]
    public decimal TotaleIva { get; set; }

    [HeaderAttribute(caption: "Firmata", Order = 18)]
    public string Firmata
    {
        get
        {
            return Caricata.MapRelTestata();
        } 
    }

    [Column("Caricata")]
    public byte Caricata { get; set; }
}