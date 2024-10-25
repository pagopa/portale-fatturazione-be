using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public sealed class FattureCommessaExcelDto
{
    [HeaderAttributev2(caption: "identificativo SC", Order = 1)]
    public string? FkIdEnte { get; set; }

    [HeaderAttributev2(caption: "ragione sociale ente", Order = 2)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "codice fiscale", Order = 3)]
    public string? CodiceFiscale { get; set; }

    [HeaderAttributev2(caption: "prodotto", Order = 4)]
    public string? Prodotto { get; set; }

    [HeaderAttributev2(caption: "Tipo Spedizione", Order = 5)]
    public string? TipoSpedizioneDigitale { get; set; } = "Digitale";

    [HeaderAttributev2(caption: "N. Notifiche NZ", Order = 6)]
    public int NumeroNotificheNazionaliDigitali { get; set; }

    [HeaderAttributev2(caption: "N. Notifiche INT", Order = 7)]
    public int NumeroNotificheInternazionaliDigitali { get; set; }

    [HeaderAttributev2(caption: "Tipo Spedizione", Order = 8)]
    public string? TipoSpedizioneAnalogicoAR { get; set; } = "analogico AR";

    [HeaderAttributev2(caption: "N. Notifiche NZ", Order = 9)]
    public int NumeroNotificheNazionaliAR { get; set; }

    [HeaderAttributev2(caption: "N. Notifiche INT", Order = 10)]
    public int NumeroNotificheInternazionaliAR { get; set; }

    [HeaderAttributev2(caption: "Tipo Spedizione", Order = 11)]
    public string? TipoSpedizioneAnalogico890 { get; set; } = "analogico 890";

    [HeaderAttributev2(caption: "N. Notifiche NZ", Order = 12)]
    public int NumeroNotificheNazionali890 { get; set; }

    [HeaderAttributev2(caption: "N. Notifiche INT", Order = 13)]
    public int NumeroNotificheInternazionali890 { get; set; }

    //-fattura
    [HeaderAttributev2(caption: "IdFattura", Order = 14)]

    [Column("IdFattura")]
    public string? IdFattura { get; set; }


    [HeaderAttributev2(caption: "Totale imponibile anticipo analogico", Order = 15)]
    public decimal ImponibileAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale imponibile anticipo digitale", Order = 16)]
    public decimal ImponibileDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Fattura anticipo", Order = 17)]
    public decimal TotaleFattura { get; set; }

    //--
    [HeaderAttributev2(caption: "Anno", Order = 18)]
    public string? AnnoValidita { get; set; }

    public int MeseValidita { get; set; }
    [HeaderAttributev2(caption: "Mese", Order = 19)]
    public string? Mese
    {
        get
        {

            return MeseValidita.GetMonth();
        }
    }

    [HeaderAttributev2(caption: "Percentuale analogico", Order = 20)]
    public decimal PercentualeA { get; set; }

    [HeaderAttributev2(caption: "Percentuale digitale", Order = 21)]
    public decimal PercentualeD { get; set; }


    [HeaderAttributev2(caption: "Totale commessa analogico", Order = 22)]
    public decimal TotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale commessa digitale", Order = 23)]
    public decimal TotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Commessa", Order = 24)]
    public decimal Totale { get; set; }
    public int IdTipoContratto { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 25)]
    public string? TipoContratto { get; set; }

    [HeaderAttributev2(caption: "N. totale notifiche analogiche", Order = 26)]
    public int TotaleNotificheAnalogico { get; set; }

    [HeaderAttributev2(caption: "N. totale notifiche digitali", Order = 27)]
    public int TotaleNotificheDigitale { get; set; }


    public bool Fatturabile { get; set; }

    [HeaderAttributev2(caption: "Fattura (si/no)", Order = 28)]
    public string? IsFattura
    {
        get
        {

            if (IdFattura is not null)
                return "SI";
            else
                return "NO";
        }
    }

    public bool? Asseverazione { get; set; }

    [HeaderAttributev2(caption: "asseverazione (si/no)", Order = 29)]
    public string? IsAsseverazione
    {
        get
        {

            if (Asseverazione is not null && Asseverazione == true)
                return "SI";
            else
                return "NO";
        }
    }

    [HeaderAttributev2(caption: "data uscita asseverazione", Order = 30)]
    public DateTime? DataUscitaAsseverazione { get; set; }

    [HeaderAttributev2(caption: "totale notifiche", Order = 31)]
    public int TotaleNotifiche { get; set; }


    [HeaderAttributev2(caption: "Stato", Order = 32)]
    public string? FkIdStato { get; set; }

}