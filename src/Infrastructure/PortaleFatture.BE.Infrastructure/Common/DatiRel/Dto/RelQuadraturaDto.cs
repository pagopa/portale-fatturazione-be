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

 
    [HeaderAttributev2(caption: "Cont. Tot. Analogico €", Order = 7)]

    [Column("ContestazioniTotaleAnalogico")]
    public decimal ContestazioniTotaleAnalogico { get; set; }
 
    [HeaderAttributev2(caption: "Cont. Tot. Digitale €", Order = 8)]

    [Column("ContestazioniTotaleDigitale")]
    public decimal ContestazioniTotaleDigitale { get; set; }
 
    [HeaderAttributev2(caption: "Cont. Tot. Not. Analogiche", Order = 9)]
    [Column("ContestazioniTotaleNotificheAnalogiche")]
    public int ContestazioniTotaleNotificheAnalogiche { get; set; } 
 
    [HeaderAttributev2(caption: "Cont. Tot. Not. Digitali", Order = 10)]

    [Column("ContestazioniTotaleNotificheDigitali")]
    public int ContestazioniTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Cont. Tot. €", Order = 11)]

    [Column("ContestazioniTotale")]
    public int ContestazioniTotale { get; set; }
 
    [HeaderAttributev2(caption: "Rel. Tot. Analogico €", Order = 13)]
    [Column("RelTotaleAnalogico")]
    public decimal RelTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Digitali €", Order = 13)]
    [Column("RelTotaleDigitale")]
    public decimal RelTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Not. Digitali", Order = 14)]
    [Column("RelTotaleNotificheAnalogiche")]
    public int RelTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Not. Digitali", Order = 15)]
    [Column("RelTotaleNotificheDigitali")]
    public int RelTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. €", Order = 16)]
    [Column("RelTotale")]
    public decimal RelTotale { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Asse. Analogico €", Order = 17)]
    [Column("RelTotaleAnalogico")]
    public decimal RelAsseTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Asse. Digitali €", Order = 18)]
    [Column("RelAsseTotaleDigitale")]
    public decimal RelAsseTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Not. Asse. Analogico", Order = 19)]
    [Column("RelAsseTotaleNotificheAnalogiche")]
    public int RelAsseTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Not. Asse. Digitali", Order = 20)]
    [Column("RelAsseTotaleNotificheDigitali")]
    public int RelAsseTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Rel. Tot. Asse. €", Order = 21)]
    [Column("RelAsseTotale")]
    public decimal RelAsseTotale { get; set; }
     


    [HeaderAttributev2(caption: "Not. Tot. Analogico €", Order = 22)]
    [Column("NotificheTotaleAnalogico")]
    public decimal NotificheTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Not. Tot. Digitali €", Order = 23)]
    [Column("NotificheTotaleDigitale")]
    public decimal NotificheTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Not. Tot. Not. Analogico", Order = 24)]
    [Column("NotificheTotaleNotificheAnalogiche")]
    public int NotificheTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Not. Tot. Not. Digitali", Order = 25)]
    [Column("NotificheTotaleNotificheDigitali")]
    public int NotificheTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Not. Tot. €", Order = 26)]
    [Column("NotificheTotale")]
    public decimal NotificheTotale { get; set; }


    [HeaderAttributev2(caption: "Diff. Tot. Analogico €", Order = 27)]
    [Column("DiffTotaleAnalogico")]
    public decimal DiffTotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Diff. Tot. Digitali €", Order = 28)]
    [Column("DiffTotaleDigitale")]
    public decimal DiffTotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Diff. Tot. Not. Analogico", Order = 29)]
    [Column("DiffTotaleNotificheAnalogiche")]
    public int DiffTotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Diff. Tot. Not. Digitali", Order = 30)]
    [Column("DiffTotaleNotificheDigitali")]
    public int DiffTotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Diff. Tot. €", Order = 31)]
    [Column("DiffTotale")]
    public decimal DiffTotale { get; set; }



    [HeaderAttributev2(caption: "Diff. Tot. Not. Zero", Order = 32)]
    [Column("DiffTotaleNotificheZero")]
    public int DiffTotaleNotificheZero { get; set; }

    [HeaderAttributev2(caption: "Tot. Not.", Order = 33)]
    [Column("TotaleNotificheCount")]
    public int TotaleNotificheCount { get; set; }

    [HeaderAttributev2(caption: "Tot. Contestate", Order = 34)]
    [Column("TotaleNotificheContestateCount")]
    public int TotaleNotificheContestateCount { get; set; }

    [HeaderAttributev2(caption: "Tot. Rel", Order = 30)]
    [Column("TotaleNotificheRelCount")]
    public int TotaleNotificheRelCount { get; set; }

}