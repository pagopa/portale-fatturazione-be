using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Core.Entities.DatiRel;

public class RelQuadraturaDto
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
 
    [HeaderAttributev2(caption: "Anno", Order = 5)]

    [Column("Anno")]
    public int? Anno { get; set; }
 
    [HeaderAttributev2(caption: "Mese", Order = 6)]

    [Column("Mese")]
    public int? Mese { get; set; }

 //contestazioni -init
    [HeaderAttributev2(caption: "Imponibile Contestazioni Analogico €", Order = 7)]

    [Column("ContestazioniTotaleAnalogico")]
    public decimal ContestazioniTotaleAnalogico { get; set; }
 
    [HeaderAttributev2(caption: "Imponibile Contestazioni Digitale €", Order = 8)]

    [Column("ContestazioniTotaleDigitale")]
    public decimal ContestazioniTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Imponibile Contestazioni  €", Order = 9)]

    [Column("ContestazioniTotale")]
    public decimal ContestazioniTotale { get; set; }

    [HeaderAttributev2(caption: "N. Contestazioni Analogiche", Order = 10)]
    [Column("ContestazioniTotaleNotificheAnalogiche")]
    public int ContestazioniTotaleNotificheAnalogiche { get; set; } 


    [HeaderAttributev2(caption: "N. Contestazioni Digitale", Order = 11)]

    [Column("ContestazioniTotaleNotificheDigitali")]
    public int ContestazioniTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "N. Totale Contestazioni", Order = 12)]

    [Column("ContestazioniNotificheTotale")]
    public int ContestazioniNotificheTotale { get; set; }
    //contestazioni -end

    //rel -init
    [HeaderAttributev2(caption: "REL Imponibile Analogico €", Order = 13)]
    [Column("RelTotaleAnalogico")]
    public decimal RelTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "REL Imponibile Digitale €", Order = 14)]
    [Column("RelTotaleDigitale")]
    public decimal RelTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "REL Imponibile €", Order = 15)]
    [Column("RelTotale")]
    public decimal RelTotale { get; set; }

    [HeaderAttributev2(caption: "N. REL Analogico", Order = 16)]
    [Column("RelTotaleNotificheAnalogiche")]
    public int RelTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "N. REL Digitale", Order = 17)]
    [Column("RelTotaleNotificheDigitali")]
    public int RelTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "N. REL Totale", Order = 18)]
    [Column("RelTotaleNotifiche")]
    public int RelTotaleNotifiche { get; set; }
    //rel -end

    //rel asse -init
    [HeaderAttributev2(caption: "Ass. Imponibile Analogico €", Order = 19)]
    [Column("RelAsseTotaleAnalogico")]
    public decimal RelAsseTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Ass. Imponibile Digitalie €", Order = 20)]
    [Column("RelAsseTotaleDigitale")]
    public decimal RelAsseTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Ass. Imponibile €", Order = 21)]
    [Column("RelAsseTotale")]
    public decimal RelAsseTotale { get; set; }

    [HeaderAttributev2(caption: "N. Asseverazione Analogico", Order = 22)]
    [Column("RelAsseTotaleNotificheAnalogiche")]
    public int RelAsseTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "N. Asseverazione Digitale", Order = 23)]
    [Column("RelAsseTotaleNotificheDigitali")]
    public int RelAsseTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "N. Asseverazione", Order = 24)]
    [Column("RelAsseTotaleNotifiche")]
    public int RelAsseTotaleNotifiche { get; set; }
    //rel asse -end 

    // notifiche --init
    [HeaderAttributev2(caption: "Consuntivo Imponible Analogico €", Order = 25)]
    [Column("NotificheTotaleAnalogico")]
    public decimal NotificheTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Consuntivo  Imponibile Digitale €", Order = 26)]
    [Column("NotificheTotaleDigitale")]
    public decimal NotificheTotaleDigitale { get; set; }


    [HeaderAttributev2(caption: "Consuntivo Imponibile €", Order = 27)]
    [Column("NotificheTotale")]
    public decimal NotificheTotale { get; set; } 

    [HeaderAttributev2(caption: "N. Consuntivo Analogico", Order = 28)]
    [Column("NotificheTotaleNotificheAnalogiche")]
    public int NotificheTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "N. Consuntivo Digitali", Order = 29)]
    [Column("NotificheTotaleNotificheDigitali")]
    public int NotificheTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "N. Totale consuntivo", Order = 30)]
    [Column("NotificheTotaleNotifiche")]
    public int NotificheTotaleNotifiche { get; set; } 


    [HeaderAttributev2(caption: "Check Imponibile Analogico €", Order = 31)]
    [Column("DiffTotaleAnalogico")]
    public decimal DiffTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Check Imponibile  Digitale €", Order = 32)]
    [Column("DiffTotaleDigitale")]
    public decimal DiffTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Check Notifiche  Analogico", Order = 33)]
    [Column("DiffTotaleNotificheAnalogiche")]
    public int DiffTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Check Notifiche  Digitali", Order = 34)]
    [Column("DiffTotaleNotificheDigitali")]
    public int DiffTotaleNotificheDigitali { get; set; } 

    [HeaderAttributev2(caption: "Check Notifiche", Order = 35)]
    [Column("DiffTotaleNotificheZero")]
    public int DiffTotaleNotificheZero { get; set; }  
}