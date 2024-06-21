﻿using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

public sealed class FattureAccontoExcelDto
{
    [HeaderAttributev2(caption: "identificativo SC", Order = 1)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "ragione sociale ente", Order = 2)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "codice fiscale", Order = 3)]
    public string? CodiceFiscale { get; set; }

    [HeaderAttributev2(caption: "prodotto", Order = 4)]
    public string? Prodotto { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 5)]
    public string? Anno { get; set; }

    public int Mese { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 6)]
    public string? MeseValidita
    {
        get
        {

            return Mese.GetMonth();
        }
    }

    [HeaderAttributev2(caption: "Tipo Spedizione", Order = 7)]
    public string? TipoSpedizioneDigitale { get; set; } = "Digitale";

    [HeaderAttributev2(caption: "N. Notifiche Digitali", Order = 8)]
    public int TotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Tipo Spedizione", Order = 9)]
    public string? TipoSpedizioneAnalogicoAR { get; set; } = "Analogico";

    [HeaderAttributev2(caption: "N. Notifiche Analogiche", Order = 10)]
    public int TotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Analogico", Order = 11)]
    public decimal TotaleAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Digitale", Order = 12)]
    public decimal TotaleDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile", Order = 13)]
    public decimal Totale { get; set; }

    //-fattura 
    public int IdTipoContratto { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 14)]
    public string? TipoContratto { get; set; }

    public decimal Percentuale { get; set; }

    [HeaderAttributev2(caption: "Percentuale Acconto", Order = 15)]
    public string? ValorePercentuale
    {
        get
        {
            return $"{Percentuale} %";
        }
    }

    [HeaderAttributev2(caption: "Totale Imponibile Acconto Analogico", Order = 16)]
    public decimal ImponibileAccontoAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Acconto Digitale", Order = 17)]
    public decimal ImponibileAccontoDigitale { get; set; }

    [HeaderAttributev2(caption: "Storno Anticipo digitale (50%)", Order = 18)]
    public decimal ImponibileStornoAnalogico { get; set; }


    [HeaderAttributev2(caption: "Storno Anticipo digitale (50%)", Order = 19)]
    public decimal ImponibileStornoDigitale { get; set; }


    [HeaderAttributev2(caption: "Totale imponibile Fattura di Acconto", Order = 20)]
    public decimal TotaleFattura { get; set; }

    public long? IdFattura { get; set; }
}