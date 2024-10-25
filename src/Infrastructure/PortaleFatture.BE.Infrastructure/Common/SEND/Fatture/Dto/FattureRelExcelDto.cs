using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class FattureRelExcelDto
{
    [Column("IdEnte")]
    [HeaderAttributev2(caption: "IdEnte", Order = 1)]
    public string? IdEnte { get; set; }


    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]

    [Column("RagioneSociale")]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "IdContratto", Order = 3)]

    [Column("IdContratto")]
    public string? IdContratto { get; set; }


    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 4)]

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }


    [HeaderAttributev2(caption: "IdFattura", Order = 4)]

    [Column("IdFattura")]
    public string? IdFattura { get; set; }


    [HeaderAttributev2(caption: "TipoDocumento", Order = 4)]

    [Column("TipoDocumento")]
    public string? TipoDocumento { get; set; }


    [HeaderAttributev2(caption: "DataFattura", Order = 4)]

    [Column("DataFattura")]
    public string? DataFattura { get; set; }


    [HeaderAttributev2(caption: "Anno", Order = 5)]

    [Column("Anno")]
    public int? Anno { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 6)]

    [Column("Mese")]
    public int? Mese { get; set; }


    //rel -init 
    [HeaderAttributev2(caption: "N. Notifiche Analogiche", Order = 7)]
    [Column("RelTotaleNotificheAnalogiche")]
    public int RelTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "N. Notifiche Digitali", Order = 8)]
    [Column("RelTotaleNotificheDigitali")]
    public int RelTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "N. Totale Notifiche", Order = 9)]
    [Column("RelTotaleNotifiche")]
    public int RelTotaleNotifiche { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Analogico €", Order = 10)]
    [Column("RelTotaleAnalogico")]
    public decimal RelTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Digitale €", Order = 11)]
    [Column("RelTotaleDigitale")]
    public decimal RelTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile €", Order = 12)]
    [Column("RelTotale")]
    public decimal RelTotale { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato Analogico €", Order = 13)]
    [Column("RelTotaleIvatoAnalogico")]
    public decimal RelTotaleIvatoAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Digitale €", Order = 14)]
    [Column("RelTotaleIvatoDigitale")]
    public decimal RelTotaleIvatoDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato Digitale €", Order = 15)]
    [Column("RelTotaleIvato")]
    public decimal RelTotaleIvato { get; set; }

    [HeaderAttributev2(caption: "Firmata", Order = 16)]
    public string Firmata
    {
        get
        {
            return Caricata.MapRelTestata();
        }
    }

    [Column("Caricata")]
    public byte Caricata { get; set; }
    //rel -end

    //storno fatture -init
    [HeaderAttributev2(caption: "Storno Anticipo Analogico €", Order = 17)]
    [Column("StornoAnticipoAnalogico")]
    public decimal StornoAnticipoAnalogico { get; set; }

    [HeaderAttributev2(caption: "Storno Anticipo Digitale €", Order = 18)]
    [Column("StornoAnticipoDigitale")]
    public decimal StornoAnticipoDigitale { get; set; }

    [HeaderAttributev2(caption: "Storno Acconto Analogico €", Order = 19)]
    [Column("StornoAccontoAnalogico")]
    public decimal StornoAccontoAnalogico { get; set; }

    [HeaderAttributev2(caption: "Storno Acconto Digitale €", Order = 20)]
    [Column("StornoAccontoDigitale")]
    public decimal StornoAccontoDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Fattura Imponibile €", Order = 21)]
    [Column("TotaleFatturaImponibile")]
    public decimal TotaleFatturaImponibile { get; set; }

    [Column("CodiceMateriale")]
    public string? CodiceMateriale { get; set; }

    [Column("RigaImponibile")]
    public decimal RigaImponibile { get; set; }
    //storno fatture -end
}